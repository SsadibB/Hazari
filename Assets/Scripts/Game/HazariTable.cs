using System.Collections;
using System.Collections.Generic;
using Hazari.AI;
using Hazari.Cards;
using Hazari.Core;
using Hazari.Players;
using Hazari.Rules;
using Hazari.Scoring;
using Hazari.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hazari.Game
{
    /// <summary>
    /// Single-player match. Updates the card Images and labels that already exist in the scene.
    /// </summary>
    public sealed class HazariTable : MonoBehaviour
    {
        static readonly string[] SeatNames = { "You", "Player 2", "Player 3", "Player 4" };

        [SerializeField] CardSpriteLibrary library;
        [SerializeField] DeckManager deck;
        [SerializeField] PlayerManager players;
        [SerializeField] CardArrangementManager arrangement;
        [SerializeField] TurnUI turnUi;
        [SerializeField] Text statusLabel;
        [SerializeField] Text sortLabel;
        [SerializeField] Text playLabel;
        [SerializeField] Button sortButton;
        [SerializeField] Button playButton;
        [SerializeField] GameObject resultPanel;
        [SerializeField] Text resultTitle;
        [SerializeField] Text resultBody;
        [SerializeField] CardView[] handCards;
        [SerializeField] Image[] opponentBacks;
        [SerializeField] Image[] tableCards;
        [SerializeField] Text[] scoreLabels;
        [SerializeField] Text[] groupLabels;
        [SerializeField] Image[] groupMarkers;

        readonly CardData[][] _hands = new CardData[HazariRules.PlayerCount][];
        readonly int[] _scores = new int[HazariRules.PlayerCount];
        CardView _lifted;
        bool _advancePressed;
        bool _waitingForHuman;
        bool _matchRunning;
        GameState _state = GameState.Waiting;

        void Start()
        {
            if (library != null)
                library.Rebuild();

            if (handCards != null)
            {
                for (var i = 0; i < handCards.Length; i++)
                {
                    if (handCards[i] == null)
                        continue;

                    handCards[i].CaptureHome();
                    handCards[i].Clicked = OnCardClicked;
                    handCards[i].DroppedOn = OnCardDropped;
                }
            }

            if (resultPanel != null)
                resultPanel.SetActive(false);

            NewMatch();
        }

        public void NewMatch()
        {
            if (_matchRunning)
                StopAllCoroutines();

            _matchRunning = false;
            _state = GameState.Dealing;
            for (var i = 0; i < _scores.Length; i++)
                _scores[i] = 0;

            if (resultPanel != null)
                resultPanel.SetActive(false);

            ClearTable();
            ShowAllBacks();

            deck.BuildStandardDeck();
            deck.Shuffle(Random.Range(1, int.MaxValue));

            var seen = new HashSet<string>();
            for (var seat = 0; seat < HazariRules.PlayerCount; seat++)
            {
                _hands[seat] = new CardData[HazariRules.CardsPerPlayer];
                for (var c = 0; c < HazariRules.CardsPerPlayer; c++)
                {
                    var card = deck.Draw();
                    if (!seen.Add(card.CardId))
                        Debug.LogError("Duplicate deal " + card.CardId);
                    _hands[seat][c] = card;
                }

                if (seat > 0)
                    _hands[seat] = ToArray(SimpleHazariAi.Arrange(_hands[seat]));

                var player = players != null ? players.GetPlayer(seat) : null;
                if (player != null)
                {
                    player.DisplayName = SeatNames[seat];
                    player.IsLocalHuman = seat == 0;
                    player.IsAi = seat != 0;
                    player.Score = 0;
                    player.Hand.SetCards(IdsOf(_hands[seat]));
                }
            }

            if (seen.Count != HazariRules.DeckSize || deck.Count != 0)
                Debug.LogError("Deal was not 52 unique cards. Seen " + seen.Count + ", left " + deck.Count);

            var dealtIds = IdsOf(_hands[0]);
            arrangement.LoadDealt(dealtIds);
            ApplyOrder(dealtIds);
            HighlightGroup(-1);
            RefreshScores();
            SetState(GameState.Arranging);
            SetStatus("Cards stay in one overlapping row. From the left they count as 3, 3, 3, then 4.");
            if (turnUi != null)
                turnUi.SetMessage("ARRANGE YOUR HAND");
        }

        public void Rematch()
        {
            NewMatch();
        }

        public void OnSortClicked()
        {
            if (_state != GameState.Arranging)
                return;

            if (arrangement.IsSorted)
                arrangement.RequestUnsort();
            else
                arrangement.RequestSort();

            ApplyOrder(arrangement.CurrentCardOrder);
            SyncHumanHandFromViews();
            RefreshControls();
        }

        public void OnPlayClicked()
        {
            if (_state == GameState.Arranging)
            {
                SyncHumanHandFromViews();
                StartCoroutine(PlayMatch());
                return;
            }

            if (_waitingForHuman)
                _advancePressed = true;
        }

        IEnumerator PlayMatch()
        {
            _matchRunning = true;
            SetState(GameState.Playing);
            ClearLift();

            for (var group = 0; group < HazariRules.GroupSizes.Length; group++)
            {
                HighlightGroup(group);
                var results = new HandResult[HazariRules.PlayerCount];

                for (var seat = 0; seat < HazariRules.PlayerCount; seat++)
                {
                    var waiting = seat == 0 ? "YOUR TURN" : "WAITING FOR " + SeatNames[seat].ToUpperInvariant();
                    if (turnUi != null)
                        turnUi.SetMessage(waiting);
                    SetStatus(SeatNames[seat] + " plays group " + (group + 1) + ".");
                    RefreshControls();

                    if (seat == 0)
                    {
                        _waitingForHuman = true;
                        _advancePressed = false;
                        RefreshControls();
                        while (!_advancePressed)
                            yield return null;
                        _waitingForHuman = false;
                        RefreshControls();
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.55f);
                    }

                    results[seat] = RevealGroup(seat, group);
                }

                AwardGroup(results);
                RefreshScores();
                SetState(GameState.RoundResult);
                yield return new WaitForSeconds(1.15f);
                ClearTable();
            }

            ShowResult();
            _matchRunning = false;
        }

        HandResult RevealGroup(int seat, int group)
        {
            var start = HazariRules.GroupStart(group);
            var size = HazariRules.GroupSizes[group];
            var cards = new CardData[size];
            for (var i = 0; i < size; i++)
                cards[i] = _hands[seat][start + i];

            for (var i = 0; i < 4; i++)
            {
                var slot = tableCards[seat * 4 + i];
                if (slot == null)
                    continue;

                if (i < size)
                {
                    slot.gameObject.SetActive(true);
                    slot.sprite = library.GetFace(cards[i].CardId);
                    slot.color = Color.white;
                    slot.preserveAspect = true;
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }

            if (seat == 0)
            {
                for (var i = 0; i < size; i++)
                {
                    if (handCards[start + i] != null)
                        handCards[start + i].gameObject.SetActive(false);
                }
            }
            else
            {
                var hidden = start + size;
                for (var i = start; i < hidden; i++)
                {
                    var back = opponentBacks[(seat - 1) * 13 + i];
                    if (back != null)
                        back.gameObject.SetActive(false);
                }
            }

            if (seat == 0)
                LayoutHandRow();

            return HandEvaluator.Evaluate(cards);
        }

        void AwardGroup(HandResult[] results)
        {
            var best = 0;
            for (var i = 1; i < results.Length; i++)
            {
                if (HazariRules.Compare(results[i], results[best]) > 0)
                    best = i;
            }

            var winners = new List<string>();
            for (var i = 0; i < results.Length; i++)
            {
                if (HazariRules.Compare(results[i], results[best]) != 0)
                    continue;

                var points = ScoreCalculator.AwardForHand(results[i]);
                _scores[i] += points;
                var player = players != null ? players.GetPlayer(i) : null;
                if (player != null)
                    player.Score = _scores[i];
                winners.Add(SeatNames[i]);
            }

            var title = winners.Count == 1 ? winners[0] + " won" : "Tie: " + string.Join(", ", winners.ToArray());
            SetStatus(title + " with " + HandEvaluator.DisplayName(results[best].Category) + ".");
        }

        void ShowResult()
        {
            SetState(GameState.MatchResult);
            var best = 0;
            for (var i = 1; i < _scores.Length; i++)
            {
                if (_scores[i] > _scores[best])
                    best = i;
            }

            var tied = 0;
            for (var i = 0; i < _scores.Length; i++)
            {
                if (_scores[i] == _scores[best])
                    tied++;
            }

            if (resultTitle != null)
            {
                if (tied > 1)
                    resultTitle.text = "MATCH TIED";
                else if (best == 0)
                    resultTitle.text = "YOU WON";
                else
                    resultTitle.text = SeatNames[best].ToUpperInvariant() + " WON";
            }

            if (resultBody != null)
            {
                var lines = new string[SeatNames.Length];
                for (var i = 0; i < SeatNames.Length; i++)
                    lines[i] = SeatNames[i].PadRight(12) + _scores[i];
                resultBody.text = string.Join("\n", lines);
            }

            if (resultPanel != null)
                resultPanel.SetActive(true);

            if (turnUi != null)
                turnUi.SetMessage("MATCH RESULT");
            RefreshControls();
        }

        void ApplyOrder(IReadOnlyList<string> cardIds)
        {
            for (var i = 0; i < handCards.Length; i++)
            {
                CardData data;
                if (!CardData.TryParse(cardIds[i], out data))
                    continue;

                handCards[i].gameObject.SetActive(true);
                handCards[i].Show(data, library.GetFace(data.CardId));
                _hands[0][i] = data;
            }

            LayoutHandRow();
            RefreshGroupLabels();
            RefreshDrag();
        }

        void LayoutHandRow()
        {
            const float cardWidth = 104f;
            const float step = 78f;
            var visible = 0;
            for (var i = 0; i < handCards.Length; i++)
            {
                if (handCards[i] != null && handCards[i].gameObject.activeSelf)
                    visible++;
            }

            if (visible == 0)
                return;

            var width = cardWidth + (visible - 1) * step;
            var x = -width * 0.5f + cardWidth * 0.5f;
            var sibling = 0;
            for (var i = 0; i < handCards.Length; i++)
            {
                if (handCards[i] == null || !handCards[i].gameObject.activeSelf)
                    continue;

                handCards[i].SetHome(new Vector2(x, 24f), sibling);
                x += step;
                sibling++;
            }
        }

        void SyncHumanHandFromViews()
        {
            var ids = new string[handCards.Length];
            for (var i = 0; i < handCards.Length; i++)
            {
                ids[i] = handCards[i].Data.CardId;
                _hands[0][i] = handCards[i].Data;
            }

            arrangement.NotifyManualOrder(ids);
            var player = players != null ? players.GetPlayer(0) : null;
            if (player != null)
                player.Hand.SetCards(ids);
        }

        void OnCardClicked(CardView view)
        {
            if (_lifted == view)
            {
                view.SetLifted(false);
                _lifted = null;
                return;
            }

            ClearLift();
            _lifted = view;
            view.SetLifted(true);
        }

        void OnCardDropped(CardView dragged, CardView target)
        {
            var draggedData = dragged.Data;
            var targetData = target.Data;
            dragged.Show(targetData, library.GetFace(targetData.CardId));
            target.Show(draggedData, library.GetFace(draggedData.CardId));
            ClearLift();
            SyncHumanHandFromViews();
            RefreshGroupLabels();
        }

        void ClearLift()
        {
            if (_lifted != null)
                _lifted.SetLifted(false);
            _lifted = null;
        }

        void RefreshDrag()
        {
            var enabled = _state == GameState.Arranging && !arrangement.IsSorted;
            for (var i = 0; i < handCards.Length; i++)
                handCards[i].DragEnabled = enabled;
        }

        void RefreshGroupLabels()
        {
            if (groupLabels == null)
                return;

            for (var group = 0; group < HazariRules.GroupSizes.Length; group++)
            {
                var start = HazariRules.GroupStart(group);
                var size = HazariRules.GroupSizes[group];
                var cards = new CardData[size];
                for (var i = 0; i < size; i++)
                    cards[i] = handCards[start + i].Data;

                if (groupLabels[group] != null)
                    groupLabels[group].text = HandEvaluator.DisplayName(HandEvaluator.Evaluate(cards).Category);
            }
        }

        void RefreshControls()
        {
            var arranging = _state == GameState.Arranging;
            if (sortButton != null)
                sortButton.interactable = arranging;
            if (sortLabel != null)
                sortLabel.text = arrangement != null && arrangement.IsSorted ? "UNSORT" : "SORT";
            if (playLabel != null)
            {
                if (arranging)
                    playLabel.text = "READY";
                else if (_state == GameState.Playing)
                    playLabel.text = "PLAY";
                else
                    playLabel.text = "PLAY";
            }

            if (playButton != null)
                playButton.interactable = arranging || _waitingForHuman;
        }

        void RefreshScores()
        {
            for (var i = 0; i < scoreLabels.Length; i++)
            {
                if (scoreLabels[i] != null)
                    scoreLabels[i].text = _scores[i].ToString();
            }
        }

        void HighlightGroup(int group)
        {
            if (groupMarkers == null)
                return;

            for (var i = 0; i < groupMarkers.Length; i++)
            {
                if (groupMarkers[i] == null)
                    continue;

                groupMarkers[i].enabled = true;
                groupMarkers[i].color = i == group
                    ? new Color(1f, 0.84f, 0.35f, 1f)
                    : new Color(0.95f, 0.78f, 0.35f, 0.35f);
            }
        }

        void ClearTable()
        {
            if (tableCards == null)
                return;

            for (var i = 0; i < tableCards.Length; i++)
            {
                if (tableCards[i] != null)
                    tableCards[i].gameObject.SetActive(false);
            }
        }

        void ShowAllBacks()
        {
            if (opponentBacks == null)
                return;

            for (var i = 0; i < opponentBacks.Length; i++)
            {
                if (opponentBacks[i] != null)
                    opponentBacks[i].gameObject.SetActive(true);
            }
        }

        void SetState(GameState state)
        {
            _state = state;
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(state);
            RefreshControls();
            RefreshDrag();
        }

        void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }

        static string[] IdsOf(CardData[] cards)
        {
            var ids = new string[cards.Length];
            for (var i = 0; i < cards.Length; i++)
                ids[i] = cards[i].CardId;
            return ids;
        }

        static CardData[] ToArray(List<CardData> cards)
        {
            var array = new CardData[cards.Count];
            for (var i = 0; i < cards.Count; i++)
                array[i] = cards[i];
            return array;
        }
    }
}
