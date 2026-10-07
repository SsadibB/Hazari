using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Hazari.AI;
using Hazari.Cards;
using Hazari.Core;
using Hazari.Players;
using Hazari.Rules;
using Hazari.Scoring;
using Hazari.UI;
using Hazari.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] Toggle autoPlayToggle;
        [SerializeField] GameObject resultPanel;
        [SerializeField] Text resultTitle;
        [SerializeField] Text resultBody;
        [SerializeField] Text[] resultNames;
        [SerializeField] Text[] resultRounds;
        [SerializeField] Text[] resultPoints;
        [SerializeField] RectTransform[] resultRows;
        [SerializeField] Button nextRoundButton;
        [SerializeField] Text nextRoundLabel;
        [SerializeField] CardView[] handCards;
        [SerializeField] Image[] opponentBacks;
        [SerializeField] Image[] tableCards;
        [SerializeField] RectTransform playedCards;
        [SerializeField] DealDirectionArrow dealArrow;
        [SerializeField] RectTransform deckPile;
        [SerializeField] RectTransform[] shuffleCards;
        [SerializeField] RectTransform[] seatRoots;
        [SerializeField] RectTransform[] handRoots;
        [SerializeField] Text[] scoreLabels;
        [SerializeField] GameObject[] decisionVisuals;
        [SerializeField] Text[] groupLabels;
        [SerializeField] Image[] groupMarkers;

        readonly CardData[][] _hands = new CardData[HazariRules.PlayerCount][];
        readonly int[] _scores = new int[HazariRules.PlayerCount];
        readonly int[] _roundScores = new int[HazariRules.PlayerCount];
        int _roundNumber;
        /// <summary>Seat index (0-3) that receives the first card this round. Advances clockwise each round.</summary>
        int _dealerSeat;
        readonly List<RectTransform> _centerCards = new List<RectTransform>();
        readonly List<Transform> _centerHomes = new List<Transform>();
        readonly int[][] _playSizes = new int[HazariRules.PlayerCount][];
        CardView _lifted;
        int _previewIndex = -1;
        bool _playerHasManuallyArranged;
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
                    handCards[i].DragMoved = OnCardDragged;
                    handCards[i].DragEnded = OnCardReleased;
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

            StartCoroutine(OpenMatch(true));
        }

        public void NextRound()
        {
            if (LeaderScore() >= HazariRules.WinningScore)
            {
                NewMatch();
                return;
            }

            if (_matchRunning)
                StopAllCoroutines();

            StartCoroutine(OpenMatch(false));
        }

        public void GoToMenu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        IEnumerator OpenMatch(bool resetScores)
        {
            _matchRunning = false;
            _previewIndex = -1;
            _state = GameState.Dealing;
            RestoreCardParents();
            if (resetScores)
            {
                _roundNumber = 0;
                _dealerSeat = 0;
                for (var i = 0; i < _scores.Length; i++)
                    _scores[i] = 0;
            }
            else
            {
                // Advance dealer one seat clockwise each round (Bottom=0 → Left=1 → Top=2 → Right=3 → …)
                _dealerSeat = (_dealerSeat + 1) % HazariRules.PlayerCount;
            }

            _roundNumber++;
            for (var i = 0; i < _roundScores.Length; i++)
                _roundScores[i] = 0;
            _playerHasManuallyArranged = false;

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
                    player.Score = _scores[seat];
                    player.Hand.SetCards(IdsOf(_hands[seat]));
                }
            }

            if (CardValueCalculator.SelfCheck() != "Card values total 360.")
                Debug.LogError(CardValueCalculator.SelfCheck());

            if (seen.Count != HazariRules.DeckSize || deck.Count != 0)
                Debug.LogError("Deal was not 52 unique cards. Seen " + seen.Count + ", left " + deck.Count);

            var dealtIds = IdsOf(_hands[0]);
            arrangement.LoadDealt(dealtIds);
            ApplyOrder(dealtIds, false, false);
            LayoutOpponentHands();
            HighlightGroup(-1);
            RefreshScores();
            SetState(GameState.Dealing);
            ShowCue(-1, null, false);
            SetStatus("Dealing from the center.");
            if (turnUi != null)
                turnUi.SetMessage("DEALING");
            RefreshControls();
            yield return DealFromCenter();
            SetState(GameState.Arranging);
            ShowCue(0, "Your Turn", true);
            SetStatus("Arrange cards or press PLAY to auto-arrange.");
            if (turnUi != null)
                turnUi.SetMessage("ARRANGE HAND");
            RefreshControls();

            if (AutoPlay)
            {
                yield return new WaitForSeconds(0.4f);
                OnPlayClicked();
            }
        }

        public void Rematch()
        {
            NextRound();
        }

        public void OnSortClicked()
        {
            if (_state != GameState.Arranging)
                return;

            _playerHasManuallyArranged = false;

            if (arrangement.IsSorted)
                arrangement.RequestUnsort();
            else
                arrangement.RequestSort();

            ApplyOrder(arrangement.CurrentCardOrder, true, false);
            SyncHumanHandFromViews();
            RefreshControls();
        }

        public void OnPlayClicked()
        {
            if (_state == GameState.Arranging)
            {
                SyncHumanHandFromViews();
                StartCoroutine(StartPlayFlow());
            }
        }

        IEnumerator StartPlayFlow()
        {
            SetState(GameState.Playing);
            ClearLift();

            // Check if human player's hand needs expert auto arrangement
            var currentCards = _hands[0];
            var g1 = EvaluateGroup(0, 0, 3);
            var g2 = EvaluateGroup(0, 3, 3);
            var g3 = EvaluateGroup(0, 6, 3);
            var isLegal = HazariRules.Compare(g1, g2) >= 0 && HazariRules.Compare(g2, g3) >= 0;

            var solved = HazariGroupArrangementSolver.Solve(currentCards);
            var shouldAutoArrange = !_playerHasManuallyArranged || !isLegal;

            // Check if current cards already match the solved arrangement
            var alreadyMatches = true;
            for (var i = 0; i < currentCards.Length; i++)
            {
                if (currentCards[i].CardId != solved.ArrangedCards[i].CardId)
                {
                    alreadyMatches = false;
                    break;
                }
            }

            if (shouldAutoArrange && !alreadyMatches)
            {
                SetStatus("Auto-arranging expert 3+3+3+4 groups...");
                if (turnUi != null)
                    turnUi.SetMessage("EXPERT ARRANGEMENT");
                yield return AnimateToArrangement(solved.ArrangedCards);
            }
            else
            {
                LayoutHandRow(true, true);
                RefreshGroupLabels();
                yield return new WaitForSeconds(0.25f);
            }

            OrderHandsForPlay();
            yield return PlayMatch();
        }

        IEnumerator AnimateToArrangement(CardData[] targetCards)
        {
            var ids = IdsOf(targetCards);
            TryReorderViews(ids);

            for (var i = 0; i < handCards.Length; i++)
            {
                if (handCards[i] == null)
                    continue;

                handCards[i].gameObject.SetActive(true);
                handCards[i].SetOwner(0);
                _hands[0][i] = handCards[i].Data;

                var targetSlot = SeatFormation.Slot(i, handCards.Length, false, SeatFormation.BottomStep, true);
                handCards[i].Rect.DOKill();
                handCards[i].Rect.DOAnchorPos(targetSlot, 0.45f).SetEase(Ease.OutCubic);
                handCards[i].Rect.DOLocalRotate(Vector3.zero, 0.45f);
                handCards[i].SetHome(targetSlot, i, false);
            }

            if (arrangement != null)
                arrangement.NotifyManualOrder(ids);
            var player = players != null ? players.GetPlayer(0) : null;
            if (player != null)
                player.Hand.SetCards(ids);

            RefreshGroupLabels();
            RefreshControls();
            yield return new WaitForSeconds(0.55f);
        }

        IEnumerator PlayMatch()
        {
            _matchRunning = true;
            if (turnUi != null)
                turnUi.SetMessage("PLAYING TRICKS");

            for (var group = 0; group < HazariRules.GroupSizes.Length; group++)
            {
                HighlightGroup(group);
                var results = new HandResult[HazariRules.PlayerCount];

                for (var seat = 0; seat < HazariRules.PlayerCount; seat++)
                {
                    ShowCue(seat, seat == 0 ? "Playing" : "Thinking", true);
                    var waiting = seat == 0 ? "YOUR TRICK" : SeatNames[seat].ToUpperInvariant() + "'S TRICK";
                    if (turnUi != null)
                        turnUi.SetMessage(AutoPlay && seat == 0 ? "AUTO PLAY" : waiting);
                    SetStatus(SeatNames[seat] + " plays " + (group == 3 ? "4-card group" : "group " + (group + 1)) + ".");
                    RefreshControls();

                    if (seat != 0)
                        yield return new WaitForSeconds(0.35f);

                    var start = GroupStart(seat, group);
                    var size = GroupSize(seat, group);
                    results[seat] = EvaluateGroup(seat, start, size);
                    yield return MoveGroupToCenter(seat, start, size);
                }

                var winner = AwardGroup(results, group);
                RefreshScores();
                ShowCue(winner, "Winner", true);
                SetState(GameState.RoundResult);
                yield return new WaitForSeconds(0.45f);
                yield return CollectCenter(winner);
                ClearTable();
            }

            var matchOver = LeaderScore() >= HazariRules.WinningScore;
            ShowResult(matchOver);
            _matchRunning = false;

            if (AutoPlay && !matchOver)
            {
                yield return new WaitForSeconds(3.5f);
                if (_state == GameState.MatchResult && LeaderScore() < HazariRules.WinningScore)
                {
                    NextRound();
                }
            }
        }

        IEnumerator DealFromCenter()
        {
            // Hands are already assigned. This moves cards from center to players.
            const float stagger = 0.06f;
            const float flight = 0.38f;

            // Arrow must stay HIDDEN for the entire deal phase.
            // Do not call ShowToward or gameObject.SetActive(true) here.
            if (dealArrow != null)
            {
                dealArrow.gameObject.SetActive(false);
            }

            // Park all 52 cards at center, invisible (scale = 0), back facing up.
            ParkDealCards();

            if (deckPile != null)
            {
                deckPile.gameObject.SetActive(true);
                deckPile.localScale = Vector3.one;
            }

            yield return ShuffleDeck();

            // Deck pile shrinks as cards leave it.
            if (deckPile != null)
                deckPile.DOScale(0.35f, HazariRules.DeckSize * stagger).SetEase(Ease.InQuad);

            // Clockwise deal order starting from _dealerSeat:
            // Seats cycle: _dealerSeat, (_dealerSeat+1)%4, (_dealerSeat+2)%4, (_dealerSeat+3)%4
            var seats = new int[HazariRules.PlayerCount];
            for (var i = 0; i < seats.Length; i++)
                seats[i] = (_dealerSeat + i) % HazariRules.PlayerCount;

            var steps = 0;
            for (var round = 0; round < HazariRules.CardsPerPlayer; round++)
            {
                for (var s = 0; s < seats.Length; s++)
                {
                    var seat = seats[s];
                    var rect = CardRect(seat, round);
                    if (rect == null)
                        continue;

                    var parent = (RectTransform)rect.parent;
                    var center = CenterInParent(parent);
                    var slot = HandSlot(seat, round);
                    var delay = steps * stagger;

                    // Capture loop variables for closure.
                    var capturedSeat = seat;
                    var capturedIndex = round;
                    var capturedStep = steps;
                    var capturedRect = rect;
                    var capturedCenter = center;

                    // Keep card INVISIBLE (scale=0) until this card's sequence actually starts.
                    // Do NOT set localScale here or all 52 cards would immediately appear as small.
                    rect.DOKill();
                    ShowDealBack(rect);          // pre-assign back sprite while still hidden
                    rect.localScale = Vector3.zero;
                    rect.anchoredPosition = center;
                    rect.localRotation = Quaternion.identity;

                    var sequence = DOTween.Sequence();
                    sequence.SetDelay(delay);
                    sequence.OnStart(() =>
                    {
                        // Card becomes visible at deck scale only when its turn comes.
                        var startScale = DealStartScale(capturedRect);
                        var wobble = ((capturedStep % 5) - 2) * 5f;
                        capturedRect.localScale = Vector3.one * startScale;
                        capturedRect.localRotation = Quaternion.Euler(0f, 0f, wobble);
                        capturedRect.anchoredPosition =
                            capturedCenter + new Vector2((capturedStep % 3) - 1f, (capturedStep % 2) * 1.5f);
                    });
                    sequence.Append(rect.DOAnchorPos(slot, flight).SetEase(Ease.OutCubic));
                    sequence.Join(rect.DOScale(1f, flight).SetEase(Ease.OutCubic));
                    sequence.Join(rect.DOLocalRotate(HandRotation(capturedSeat), flight));
                    sequence.OnComplete(() => RevealDealtCard(capturedSeat, capturedIndex));
                    steps++;
                }
            }

            // Wait for every card to finish animating.
            yield return new WaitForSeconds(steps * stagger + flight + 0.4f);

            // --- Full center cleanup: no stray card should remain ---

            // Hide deckPile.
            if (deckPile != null)
            {
                deckPile.DOKill();
                deckPile.localScale = Vector3.zero;
                deckPile.gameObject.SetActive(false);
            }

            // Ensure all shuffleCards are inactive.
            if (shuffleCards != null)
            {
                for (var i = 0; i < shuffleCards.Length; i++)
                {
                    if (shuffleCards[i] != null)
                    {
                        shuffleCards[i].DOKill();
                        shuffleCards[i].gameObject.SetActive(false);
                    }
                }
            }

            // Arrow stays hidden throughout the deal phase.
            // It will only be shown when CollectCenter is called (trick collection).
        }

        void ParkDealCards()
        {
            // Park every deal card at the center, completely invisible.
            // Cards stay at scale=0 until their individual DOTween sequence fires (in OnStart).
            for (var seat = 0; seat < HazariRules.PlayerCount; seat++)
            {
                for (var index = 0; index < HazariRules.CardsPerPlayer; index++)
                {
                    var rect = CardRect(seat, index);
                    if (rect == null)
                        continue;

                    var parent = (RectTransform)rect.parent;
                    rect.DOKill();
                    rect.anchoredPosition = CenterInParent(parent);
                    rect.localScale = Vector3.zero;   // invisible – remains zero until OnStart
                    rect.localRotation = Quaternion.identity;
                    ShowDealBack(rect);
                }
            }
        }

        IEnumerator ShuffleDeck()
        {
            if (shuffleCards == null || shuffleCards.Length == 0)
                yield break;

            for (var i = 0; i < shuffleCards.Length; i++)
            {
                var card = shuffleCards[i];
                if (card == null)
                    continue;

                card.gameObject.SetActive(true);
                card.DOKill();
                card.anchoredPosition = Vector2.zero;
                card.localRotation = Quaternion.identity;
                card.localScale = Vector3.one;
                var spread = (i - (shuffleCards.Length - 1) * 0.5f) * 45f;
                card.DOAnchorPos(new Vector2(spread, 12f), 0.24f).SetEase(Ease.OutCubic);
                card.DOLocalRotate(new Vector3(0f, 0f, spread * 0.35f), 0.24f);
            }

            yield return new WaitForSeconds(0.28f);

            for (var i = 0; i < shuffleCards.Length; i++)
            {
                var card = shuffleCards[i];
                if (card == null)
                    continue;

                card.DOAnchorPos(new Vector2(i * 2.5f, i * 2.5f), 0.2f).SetEase(Ease.InCubic);
                card.DOLocalRotate(Vector3.zero, 0.2f);
            }

            yield return new WaitForSeconds(0.22f);

            for (var i = 0; i < shuffleCards.Length; i++)
            {
                if (shuffleCards[i] != null)
                    shuffleCards[i].gameObject.SetActive(false);
            }
        }

        void ShowDealBack(RectTransform rect)
        {
            if (library == null || library.CardBack == null || rect == null)
                return;

            var view = rect.GetComponent<CardView>();
            if (view != null)
            {
                view.Present(library.CardBack);
                return;
            }

            var image = rect.GetComponent<Image>();
            if (image != null)
                image.sprite = library.CardBack;
        }

        void RevealDealtCard(int seat, int index)
        {
            if (seat != 0 || handCards == null || index >= handCards.Length || handCards[index] == null)
                return;

            var view = handCards[index];
            if (string.IsNullOrEmpty(view.Data.CardId))
                return;

            var face = library.GetFace(view.Data.CardId);
            var rect = view.Rect;
            rect.DOKill();
            rect.localScale = Vector3.one;

            // Card reached hand with back visible.
            // Smoothly flip around Y axis using DOTween:
            // 1. Rotate Y to 90 degrees (card turns edge-on)
            // 2. Change sprite to card face at edge-on
            // 3. Rotate Y from -90 degrees back to 0 degrees to reveal front face
            var flip = DOTween.Sequence();
            flip.Append(rect.DOLocalRotate(new Vector3(0f, 90f, 0f), 0.14f).SetEase(Ease.InQuad));
            flip.AppendCallback(() =>
            {
                view.Present(face);
                rect.localRotation = Quaternion.Euler(0f, -90f, 0f);
            });
            flip.Append(rect.DOLocalRotate(Vector3.zero, 0.16f).SetEase(Ease.OutQuad));
            flip.OnComplete(() =>
            {
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            });
        }

        static float DealStartScale(RectTransform rect)
        {
            var height = rect.sizeDelta.y;
            if (height < 1f)
                return 0.7f;
            return Mathf.Clamp(160f / height, 0.5f, 1.8f);
        }

        RectTransform CardRect(int seat, int index)
        {
            if (seat == 0)
                return handCards != null && handCards[index] != null ? handCards[index].Rect : null;

            if (opponentBacks == null)
                return null;

            var back = opponentBacks[(seat - 1) * HazariRules.CardsPerPlayer + index];
            return back != null ? back.rectTransform : null;
        }

        Vector2 HandSlot(int seat, int index)
        {
            if (seat == 0)
                return SeatFormation.Slot(index, HazariRules.CardsPerPlayer, false, SeatFormation.BottomStep, false);
            if (seat == 2)
                return SeatFormation.Slot(index, HazariRules.CardsPerPlayer, false, SeatFormation.TopStep, false);
            return SeatFormation.Slot(index, HazariRules.CardsPerPlayer, true, SeatFormation.SideStep, false);
        }

        static Vector3 HandRotation(int seat)
        {
            if (seat == 1)
                return new Vector3(0f, 0f, SeatFormation.LeftRotation);
            if (seat == 2)
                return new Vector3(0f, 0f, SeatFormation.TopRotation);
            if (seat == 3)
                return new Vector3(0f, 0f, SeatFormation.RightRotation);
            return Vector3.zero;
        }

        Vector2 CenterInParent(RectTransform parent)
        {
            if (playedCards == null || parent == null)
                return Vector2.zero;

            var world = playedCards.TransformPoint(Vector3.zero);
            return parent.InverseTransformPoint(world);
        }

        bool AutoPlay
        {
            get { return autoPlayToggle != null && autoPlayToggle.isOn; }
        }

        void OrderHandsForPlay()
        {
            // Human player: ensure 3-card groups are strongest first, and 4-card group is last
            int[] humanSizes;
            GroupOrder.OrderStrongestFirst(_hands[0], out humanSizes);
            _playSizes[0] = humanSizes;
            var humanPlayer = players != null ? players.GetPlayer(0) : null;
            if (humanPlayer != null)
                humanPlayer.Hand.SetCards(IdsOf(_hands[0]));

            ApplyOrder(IdsOf(_hands[0]), true, true);
            if (arrangement != null)
                arrangement.NotifyManualOrder(IdsOf(_hands[0]));

            // Opponent AI players
            for (var seat = 1; seat < HazariRules.PlayerCount; seat++)
            {
                _hands[seat] = ToArray(SimpleHazariAi.Arrange(_hands[seat]));
                int[] sizes;
                GroupOrder.OrderStrongestFirst(_hands[seat], out sizes);
                _playSizes[seat] = sizes;
                var player = players != null ? players.GetPlayer(seat) : null;
                if (player != null)
                    player.Hand.SetCards(IdsOf(_hands[seat]));
            }
        }

        int GroupSize(int seat, int group)
        {
            if (_playSizes[seat] == null || group >= _playSizes[seat].Length)
                return HazariRules.GroupSizes[group];
            return _playSizes[seat][group];
        }

        int GroupStart(int seat, int group)
        {
            var start = 0;
            for (var i = 0; i < group; i++)
                start += GroupSize(seat, i);
            return start;
        }

        HandResult EvaluateGroup(int seat, int start, int size)
        {
            var cards = new CardData[size];
            for (var i = 0; i < size; i++)
                cards[i] = _hands[seat][start + i];
            return HandEvaluator.Evaluate(cards);
        }

        IEnumerator MoveGroupToCenter(int seat, int start, int size)
        {
            if (playedCards == null)
                yield break;

            var origin = CenterOrigin(seat);
            var longest = 0f;
            for (var i = 0; i < size; i++)
            {
                var rect = TakeGroupCard(seat, start + i);
                if (rect == null)
                    continue;

                if (seat != 0)
                {
                    var image = rect.GetComponent<Image>();
                    if (image != null)
                    {
                        image.sprite = library.GetFace(_hands[seat][start + i].CardId);
                        image.color = Color.white;
                    }
                }

                rect.localScale = Vector3.one;
                var target = origin + new Vector2((i - (size - 1) * 0.5f) * SeatFormation.CenterStep, 0f);
                var lift = rect.anchoredPosition + new Vector2(0f, 28f);
                rect.DOKill();
                var sequence = DOTween.Sequence();
                sequence.Append(rect.DOAnchorPos(lift, 0.08f));
                sequence.Append(rect.DOAnchorPos(target, 0.4f).SetEase(Ease.OutCubic));
                sequence.Join(rect.DOLocalRotate(Vector3.zero, 0.4f));
                sequence.Join(DOTween.To(() => rect.sizeDelta, value => rect.sizeDelta = value, SeatFormation.CenterCardSize, 0.4f));
                if (sequence.Duration() > longest)
                    longest = sequence.Duration();
            }

            if (seat == 0)
                LayoutHandRow(true);
            else
                LayoutOpponentHands();

            yield return new WaitForSeconds(Mathf.Max(0.5f, longest));
        }

        RectTransform TakeGroupCard(int seat, int index)
        {
            RectTransform rect = null;
            if (seat == 0)
            {
                if (handCards == null || handCards[index] == null)
                    return null;
                rect = handCards[index].Rect;
            }
            else if (opponentBacks != null)
            {
                var back = opponentBacks[(seat - 1) * HazariRules.CardsPerPlayer + index];
                if (back != null)
                    rect = back.rectTransform;
            }

            if (rect == null || playedCards == null)
                return null;

            _centerHomes.Add(rect.parent);
            _centerCards.Add(rect);
            rect.SetParent(playedCards, true);
            rect.SetAsLastSibling();
            return rect;
        }

        IEnumerator CollectCenter(int winnerSeat)
        {
            if (_centerCards.Count == 0)
                yield break;

            var target = seatRoots != null && winnerSeat >= 0 && winnerSeat < seatRoots.Length && seatRoots[winnerSeat] != null
                ? seatRoots[winnerSeat].position
                : playedCards.position;

            if (dealArrow != null)
                dealArrow.ShowToward(winnerSeat);

            for (var i = 0; i < _centerCards.Count; i++)
            {
                var rect = _centerCards[i];
                if (rect == null)
                    continue;

                rect.DOKill();
                rect.DOMove(target, 0.5f).SetEase(Ease.InCubic);
                rect.DOScale(0.4f, 0.5f);
            }

            yield return new WaitForSeconds(0.52f);
            if (dealArrow != null)
                dealArrow.Hide();

            for (var i = 0; i < _centerCards.Count; i++)
            {
                var rect = _centerCards[i];
                if (rect == null)
                    continue;

                rect.DOKill();
                if (_centerHomes[i] != null)
                    rect.SetParent(_centerHomes[i], false);
                rect.localScale = Vector3.one;
                rect.gameObject.SetActive(false);
            }

            _centerCards.Clear();
            _centerHomes.Clear();
        }

        static Vector2 CenterOrigin(int seat)
        {
            switch (seat)
            {
                case 1: return new Vector2(-222f, 16f);
                case 2: return new Vector2(0f, 171f);
                case 3: return new Vector2(222f, 16f);
                default: return new Vector2(0f, -139f);
            }
        }

        int AwardGroup(HandResult[] results, int group)
        {
            var best = 0;
            for (var i = 1; i < results.Length; i++)
            {
                if (HazariRules.Compare(results[i], results[best]) > 0)
                    best = i;
            }

            var pot = 0;
            for (var seat = 0; seat < results.Length; seat++)
            {
                var start = GroupStart(seat, group);
                var size = GroupSize(seat, group);
                for (var i = 0; i < size; i++)
                    pot += CardValueCalculator.Points(_hands[seat][start + i]);
            }

            var winners = new List<int>();
            for (var i = 0; i < results.Length; i++)
            {
                if (HazariRules.Compare(results[i], results[best]) == 0)
                    winners.Add(i);
            }

            var share = winners.Count == 0 ? 0 : pot / winners.Count;
            var remainder = winners.Count == 0 ? 0 : pot % winners.Count;
            var names = new List<string>();
            for (var i = 0; i < winners.Count; i++)
            {
                var seat = winners[i];
                var points = share + (i < remainder ? 1 : 0);
                _roundScores[seat] += points;
                _scores[seat] += points;
                var player = players != null ? players.GetPlayer(seat) : null;
                if (player != null)
                    player.Score = _scores[seat];
                names.Add(SeatNames[seat]);
                if (scoreLabels != null && seat < scoreLabels.Length && scoreLabels[seat] != null)
                {
                    scoreLabels[seat].transform.DOKill();
                    scoreLabels[seat].transform.localScale = Vector3.one;
                    scoreLabels[seat].transform.DOScale(1.12f, 0.12f).SetLoops(2, LoopType.Yoyo);
                }
            }

            var title = names.Count == 1 ? names[0] + " took " + pot : "Tie: " + string.Join(", ", names.ToArray());
            SetStatus(title + " with " + HandEvaluator.DisplayName(results[best].Category) + ".");
            return winners.Count > 0 ? winners[0] : 0;
        }

        int LeaderScore()
        {
            var best = 0;
            for (var i = 1; i < _scores.Length; i++)
            {
                if (_scores[i] > best)
                    best = _scores[i];
            }

            return best;
        }

        void ShowResult(bool matchOver)
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
                if (!matchOver)
                    resultTitle.text = "ROUND " + _roundNumber + " RESULT";
                else if (tied > 1)
                    resultTitle.text = "MATCH TIED";
                else if (best == 0)
                    resultTitle.text = "YOU WON THE MATCH!";
                else
                    resultTitle.text = SeatNames[best].ToUpperInvariant() + " WINS THE MATCH!";
            }

            if (resultBody != null)
            {
                resultBody.text = matchOver
                    ? "Winner reached " + _scores[best] + " total points! (Goal: 1000)"
                    : "Totals carry forward. First player to 1000 wins!";
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.gameObject.SetActive(true);
                if (nextRoundLabel != null)
                    nextRoundLabel.text = matchOver ? "PLAY AGAIN" : "NEXT ROUND";
            }

            for (var i = 0; i < SeatNames.Length; i++)
            {
                if (resultNames != null && i < resultNames.Length && resultNames[i] != null)
                    resultNames[i].text = SeatNames[i];

                var row = resultRows != null && i < resultRows.Length ? resultRows[i] : null;
                if (row != null)
                {
                    row.DOKill();
                    row.localScale = Vector3.one * 0.92f;
                    row.DOScale(1f, 0.22f).SetDelay(0.08f * i).SetEase(Ease.OutCubic);
                }

                if (resultRounds != null && i < resultRounds.Length && resultRounds[i] != null)
                    resultRounds[i].text = "+" + _roundScores[i];

                if (resultPoints != null && i < resultPoints.Length && resultPoints[i] != null)
                {
                    var label = resultPoints[i];
                    var score = _scores[i];
                    var shown = score - _roundScores[i];
                    label.text = shown.ToString();
                    label.transform.DOKill();
                    label.transform.localScale = Vector3.one;
                    DOTween.To(() => shown, value =>
                    {
                        shown = value;
                        label.text = shown.ToString();
                    }, score, 0.6f).SetDelay(0.12f * i).SetEase(Ease.OutCubic);
                    label.color = (i == best && matchOver)
                        ? new Color(1f, 0.84f, 0.28f)
                        : (i == best ? new Color(0.95f, 0.9f, 0.6f) : Color.white);
                    if (matchOver && i == best)
                        label.transform.DOScale(1.15f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetDelay(0.6f);
                }
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.one * 0.9f;
                resultPanel.transform.DOScale(1f, 0.28f).SetEase(Ease.OutCubic);
            }

            if (turnUi != null)
                turnUi.SetMessage(matchOver ? "MATCH OVER" : "ROUND " + _roundNumber);
            ShowCue(-1, null, false);
            RefreshControls();
        }

        void ApplyOrder(IReadOnlyList<string> cardIds, bool animate, bool groupGaps = false)
        {
            if (!TryReorderViews(cardIds))
            {
                for (var i = 0; i < handCards.Length; i++)
                {
                    CardData data;
                    if (!CardData.TryParse(cardIds[i], out data))
                        continue;

                    handCards[i].gameObject.SetActive(true);
                    handCards[i].Show(data, library.GetFace(data.CardId));
                    handCards[i].SetOwner(0);
                    _hands[0][i] = data;
                }
            }
            else
            {
                for (var i = 0; i < handCards.Length; i++)
                {
                    handCards[i].gameObject.SetActive(true);
                    handCards[i].SetOwner(0);
                    _hands[0][i] = handCards[i].Data;
                }
            }

            LayoutHandRow(animate, groupGaps);
            RefreshGroupLabels();
            RefreshDrag();
        }

        bool TryReorderViews(IReadOnlyList<string> cardIds)
        {
            if (handCards == null || cardIds == null || cardIds.Count != handCards.Length)
                return false;

            var next = new CardView[handCards.Length];
            var used = new bool[handCards.Length];
            for (var i = 0; i < cardIds.Count; i++)
            {
                var found = -1;
                for (var view = 0; view < handCards.Length; view++)
                {
                    if (used[view] || handCards[view] == null || handCards[view].Data.CardId != cardIds[i])
                        continue;

                    found = view;
                    break;
                }

                if (found < 0)
                    return false;

                used[found] = true;
                next[i] = handCards[found];
            }

            for (var i = 0; i < next.Length; i++)
                handCards[i] = next[i];
            return true;
        }

        void LayoutHandRow(bool animate, bool groupGaps = false)
        {
            if (handCards == null)
                return;

            var visible = new List<CardView>();
            for (var i = 0; i < handCards.Length; i++)
            {
                if (handCards[i] == null || !handCards[i].gameObject.activeSelf)
                    continue;
                if (!IsInHand(handCards[i].transform))
                    continue;
                visible.Add(handCards[i]);
            }

            for (var i = 0; i < visible.Count; i++)
            {
                var rect = visible[i].Rect;
                rect.sizeDelta = SeatFormation.BottomCardSize;
                if (!visible[i].IsDragging)
                    rect.localRotation = Quaternion.identity;
                var slot = SeatFormation.Slot(i, visible.Count, false, SeatFormation.BottomStep, groupGaps);
                visible[i].SetHome(slot, i, animate && !visible[i].IsDragging);
            }
        }

        void LayoutOpponentHands()
        {
            if (opponentBacks == null || opponentBacks.Length < HazariRules.CardsPerPlayer * 3)
                return;

            LayoutBacks(0, true, SeatFormation.SideCardSize, SeatFormation.SideStep, SeatFormation.LeftRotation);
            LayoutBacks(HazariRules.CardsPerPlayer, false, SeatFormation.TopCardSize, SeatFormation.TopStep, SeatFormation.TopRotation);
            LayoutBacks(HazariRules.CardsPerPlayer * 2, true, SeatFormation.SideCardSize, SeatFormation.SideStep, SeatFormation.RightRotation);
        }

        void LayoutBacks(int start, bool vertical, Vector2 size, float step, float rotation)
        {
            var visible = new System.Collections.Generic.List<RectTransform>();
            for (var i = 0; i < HazariRules.CardsPerPlayer; i++)
            {
                var image = opponentBacks[start + i];
                if (image != null && image.gameObject.activeSelf && IsInHand(image.transform))
                    visible.Add(image.rectTransform);
            }

            for (var i = 0; i < visible.Count; i++)
                SeatFormation.Place(visible[i], i, visible.Count, vertical, size, step, rotation);
        }

        void ShowCue(int seat, string message, bool pulse)
        {
            if (decisionVisuals == null)
                return;

            for (var i = 0; i < decisionVisuals.Length; i++)
            {
                if (decisionVisuals[i] == null)
                    continue;

                var cue = decisionVisuals[i].GetComponent<SeatDecisionView>();
                if (cue != null)
                    cue.Show(i == seat ? message : null, pulse && i == seat);
                else
                    decisionVisuals[i].SetActive(i == seat);
            }
        }

        bool IsInHand(Transform card)
        {
            return card != null && card.parent != null && card.parent.name == "HandCards";
        }

        void RestoreCardParents()
        {
            _centerCards.Clear();
            _centerHomes.Clear();
            if (handCards != null && handRoots != null && handRoots.Length > 0 && handRoots[0] != null)
            {
                for (var i = 0; i < handCards.Length; i++)
                {
                    if (handCards[i] == null)
                        continue;

                    handCards[i].transform.DOKill();
                    handCards[i].transform.SetParent(handRoots[0], false);
                    handCards[i].transform.localScale = Vector3.one;
                    handCards[i].gameObject.SetActive(true);
                }
            }

            if (opponentBacks == null || handRoots == null)
                return;

            for (var i = 0; i < opponentBacks.Length; i++)
            {
                if (opponentBacks[i] == null)
                    continue;

                var seat = 1 + i / HazariRules.CardsPerPlayer;
                if (seat >= handRoots.Length || handRoots[seat] == null)
                    continue;

                var rect = opponentBacks[i].rectTransform;
                rect.DOKill();
                rect.SetParent(handRoots[seat], false);
                rect.localScale = Vector3.one;
                opponentBacks[i].gameObject.SetActive(true);
                if (library != null && library.CardBack != null)
                    opponentBacks[i].sprite = library.CardBack;
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

        void OnCardDragged(CardView view)
        {
            if (_state != GameState.Arranging || view == null)
                return;

            var insert = InsertionIndex(view);
            if (insert == _previewIndex)
                return;

            _previewIndex = insert;
            LayoutPreview(view, insert);
        }

        void OnCardReleased(CardView view)
        {
            if (view == null)
                return;

            _previewIndex = -1;
            if (_state != GameState.Arranging)
            {
                LayoutHandRow(true, true);
                return;
            }

            var ordered = new List<CardView>();
            for (var i = 0; i < handCards.Length; i++)
            {
                if (handCards[i] != null && handCards[i] != view && handCards[i].gameObject.activeSelf && IsInHand(handCards[i].transform))
                    ordered.Add(handCards[i]);
            }

            var insert = Mathf.Clamp(InsertionIndex(view), 0, ordered.Count);
            ordered.Insert(insert, view);
            for (var i = 0; i < handCards.Length && i < ordered.Count; i++)
                handCards[i] = ordered[i];

            _playerHasManuallyArranged = true;
            ClearLift();
            SyncHumanHandFromViews();
            if (arrangement != null)
                arrangement.AcceptRearrange(arrangement.CurrentCardOrder);
            LayoutHandRow(true, true);
            RefreshGroupLabels();
            RefreshControls();
        }

        void LayoutPreview(CardView dragged, int insert)
        {
            var ordered = new List<CardView>();
            for (var i = 0; i < handCards.Length; i++)
            {
                if (handCards[i] != null && handCards[i] != dragged && handCards[i].gameObject.activeSelf && IsInHand(handCards[i].transform))
                    ordered.Add(handCards[i]);
            }

            insert = Mathf.Clamp(insert, 0, ordered.Count);
            ordered.Insert(insert, dragged);
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] == dragged)
                    continue;

                var slot = SeatFormation.Slot(i, ordered.Count, false, SeatFormation.BottomStep, true);
                ordered[i].SetHome(slot, i, true);
            }
        }

        int InsertionIndex(CardView view)
        {
            var count = 0;
            for (var i = 0; i < handCards.Length; i++)
            {
                if (handCards[i] != null && handCards[i].gameObject.activeSelf && (handCards[i] == view || IsInHand(handCards[i].transform)))
                    count++;
            }

            if (count == 0)
                return 0;

            var x = view.Rect.anchoredPosition.x;
            var best = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var slot = SeatFormation.Slot(i, count, false, SeatFormation.BottomStep, true);
                var distance = Mathf.Abs(slot.x - x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        void ClearLift()
        {
            if (_lifted != null)
                _lifted.SetLifted(false);
            _lifted = null;
        }

        void RefreshDrag()
        {
            var enabled = _state == GameState.Arranging;
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
                playLabel.text = "PLAY";

            if (playButton != null)
                playButton.interactable = arranging;
        }

        void RefreshScores()
        {
            // Avatar labels show only this round's points; cumulative totals live in the result panel.
            for (var i = 0; i < scoreLabels.Length; i++)
            {
                if (scoreLabels[i] != null)
                    scoreLabels[i].text = _roundScores[i].ToString();
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
