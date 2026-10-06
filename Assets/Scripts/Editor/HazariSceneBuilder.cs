using System.Collections.Generic;
using System.IO;
using Hazari.Cards;
using Hazari.Game;
using Hazari.Players;
using Hazari.Rules;
using Hazari.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hazari.EditorTools
{
    public static class HazariSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Game/Game.unity";

        static readonly string[] SampleHand =
        {
            "AS", "AH", "AD", "KS", "QS", "JS", "8H", "8C", "5D", "10S", "7H", "3D", "2C"
        };

        [MenuItem("Hazari/Build Game Table")]
        public static void Build()
        {
            var faces = LoadFaces();
            var back = LoadSprite("Assets/Cards/Card_Back.png");
            var avatars = new[]
            {
                LoadSprite("Assets/Avatars/Avatar1.png"),
                LoadSprite("Assets/Avatars/Avatar2.png"),
                LoadSprite("Assets/Avatars/Avatar3.png"),
                LoadSprite("Assets/Avatars/Avatar4.png")
            };
            var panel = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.16f, 0.22f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasObject = new GameObject("HazariCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var root = canvasObject.transform;
            var background = CreateImage(root, "Background", panel, new Color(0.03f, 0.22f, 0.28f), Vector2.zero, new Vector2(1920f, 1080f));
            StretchFull((RectTransform)background.transform);

            var table = new GameObject("GameTable", typeof(RectTransform));
            table.transform.SetParent(root, false);
            Stretch((RectTransform)table.transform);
            var library = table.AddComponent<CardSpriteLibrary>();
            var deck = table.AddComponent<DeckManager>();
            var playerManager = table.AddComponent<PlayerManager>();
            var arrangement = table.AddComponent<CardArrangementManager>();
            var turnUi = table.AddComponent<TurnUI>();
            var match = table.AddComponent<HazariTable>();

            var entries = new List<CardSpriteLibrary.Entry>();
            foreach (var pair in faces)
                entries.Add(new CardSpriteLibrary.Entry { cardId = pair.Key, sprite = pair.Value });

            var glow = CreateImage(table.transform, "TableGlow", panel, new Color(0.95f, 0.62f, 0.18f, 0.22f), new Vector2(0f, 24f), new Vector2(1280f, 720f));
            glow.GetComponent<Image>().raycastTarget = false;
            var boardSprite = LoadSprite("Assets/Art/Table/board.png");
            var board = CreateImage(table.transform, "Board", boardSprite, Color.white, new Vector2(0f, 24f), new Vector2(1120f, 595f));
            var boardImage = board.GetComponent<Image>();
            boardImage.preserveAspect = true;
            boardImage.raycastTarget = false;

            var center = CreateRect(table.transform, "CenterArea", new Vector2(0f, 24f), new Vector2(560f, 380f));
            var played = CreateRect(center, "PlayedCards", Vector2.zero, new Vector2(560f, 380f));

            var players = CreateRect(table.transform, "Players", Vector2.zero, Vector2.zero);
            StretchFull(players);

            var hand = new CardView[13];
            var backs = new Image[39];
            var scoreLabels = new Text[4];
            var decisions = new GameObject[4];

            var bottom = CreateRect(players, "BottomPlayer", new Vector2(0f, -300f), new Vector2(1400f, 380f));
            var bottomHand = CreateRect(bottom, "HandCards", Vector2.zero, new Vector2(1380f, 200f));
            PlaceHand(bottomHand, hand, faces);
            CreateAvatar(bottom, avatars[0], new Vector2(0f, -136f), 60f);
            CreateName(bottom, font, "You", 20, new Vector2(0f, -178f), new Vector2(240f, 26f));
            scoreLabels[0] = CreateScore(bottom, font, 18, new Vector2(0f, -206f), new Vector2(280f, 26f));
            decisions[0] = CreateDecision(bottom, font, new Vector2(78f, -178f));

            var top = CreateRect(players, "TopPlayer", new Vector2(0f, 424f), new Vector2(360f, 220f));
            CreateAvatar(top, avatars[2], Vector2.zero, 52f);
            CreateName(top, font, "Player 3", 16, new Vector2(0f, -42f), new Vector2(200f, 24f));
            scoreLabels[2] = CreateScore(top, font, 14, new Vector2(0f, -64f), new Vector2(210f, 22f));
            var topHand = CreateRect(top, "HandCards", new Vector2(0f, -112f), new Vector2(280f, 70f));
            PlaceBacks(topHand, backs, back, 13, false, SeatFormation.TopCardSize, SeatFormation.TopStep, SeatFormation.TopRotation);
            decisions[2] = CreateDecision(top, font, new Vector2(78f, -42f));

            var left = CreateRect(players, "LeftPlayer", new Vector2(-704f, 103f), new Vector2(280f, 280f));
            CreateAvatar(left, avatars[1], Vector2.zero, 56f);
            CreateName(left, font, "Player 2", 16, new Vector2(0f, -38f), new Vector2(150f, 24f));
            scoreLabels[1] = CreateScore(left, font, 14, new Vector2(0f, -64f), new Vector2(156f, 36f));
            var leftHand = CreateRect(left, "HandCards", new Vector2(128f, -79f), new Vector2(80f, 230f));
            PlaceBacks(leftHand, backs, back, 0, true, SeatFormation.SideCardSize, SeatFormation.SideStep, SeatFormation.LeftRotation);
            decisions[1] = CreateDecision(left, font, new Vector2(0f, -92f));

            var right = CreateRect(players, "RightPlayer", new Vector2(704f, 103f), new Vector2(280f, 280f));
            var rightHand = CreateRect(right, "HandCards", new Vector2(-128f, -79f), new Vector2(80f, 230f));
            PlaceBacks(rightHand, backs, back, 26, true, SeatFormation.SideCardSize, SeatFormation.SideStep, SeatFormation.RightRotation);
            CreateAvatar(right, avatars[3], Vector2.zero, 56f);
            CreateName(right, font, "Player 4", 16, new Vector2(0f, -38f), new Vector2(150f, 24f));
            scoreLabels[3] = CreateScore(right, font, 14, new Vector2(0f, -64f), new Vector2(156f, 36f));
            decisions[3] = CreateDecision(right, font, new Vector2(0f, -92f));
            center.SetAsLastSibling();

            var tableCards = new Image[16];
            CreatePlayRow(board.transform, tableCards, 0, new Vector2(0f, -108f));
            CreatePlayRow(board.transform, tableCards, 4, new Vector2(-196f, 0f));
            CreatePlayRow(board.transform, tableCards, 8, new Vector2(0f, 108f));
            CreatePlayRow(board.transform, tableCards, 12, new Vector2(196f, 0f));

            var header = CreateImage(root, "Header", panel, new Color(0.05f, 0.28f, 0.34f, 0.96f), Vector2.zero, new Vector2(1920f, 52f));
            AnchorTopStretch((RectTransform)header.transform, 52f);
            var title = CreateText(root, "Title", font, "HAZARI", 36, new Color(1f, 0.84f, 0.35f), Vector2.zero, new Vector2(280f, 44f), FontStyle.Bold);
            AnchorTopLeft((RectTransform)title.transform, new Vector2(24f, -4f), new Vector2(280f, 44f));
            title.alignment = TextAnchor.MiddleLeft;
            AddShadow(title);

            var turn = CreateText(root, "Turn", font, "ARRANGE YOUR HAND", 22, new Color(1f, 0.9f, 0.55f), Vector2.zero, new Vector2(520f, 40f), FontStyle.Bold);
            AnchorTopRight((RectTransform)turn.transform, new Vector2(-24f, -6f), new Vector2(520f, 40f));
            turn.alignment = TextAnchor.MiddleRight;
            AddShadow(turn);
            SetRef(turnUi, "label", turn);

            var status = CreateText(root, "Status", font, "One overlapping row. From the left: 3, 3, 3, then 4.", 18, new Color(1f, 0.95f, 0.82f), Vector2.zero, new Vector2(460f, 40f), FontStyle.Bold);
            AnchorBottomLeft((RectTransform)status.transform, new Vector2(20f, 18f), new Vector2(460f, 40f));
            status.alignment = TextAnchor.MiddleLeft;
            AddShadow(status);

            var sortButton = CreateButton(root, "SortButton", panel, font, "SORT", new Vector2(620f, -500f), new Color(0.15f, 0.65f, 0.72f), out var sortLabel);
            var playButton = CreateButton(root, "PlayButton", panel, font, "PLAY", new Vector2(860f, -500f), new Color(0.9f, 0.35f, 0.38f), out var playLabel);
            var autoPlay = CreateAutoPlay(root, panel, font);
            AnchorBottomRight((RectTransform)playButton.transform, new Vector2(-24f, 20f), new Vector2(210f, 64f));
            AnchorBottomRight((RectTransform)sortButton.transform, new Vector2(-250f, 20f), new Vector2(210f, 64f));
            AnchorBottomRight((RectTransform)autoPlay.transform, new Vector2(-490f, 28f), new Vector2(210f, 48f));

            var resultNames = new Text[4];
            var resultPoints = new Text[4];
            var resultRows = new RectTransform[4];
            var result = CreateImage(root, "ResultPanel", panel, new Color(0.05f, 0.16f, 0.28f, 0.97f), Vector2.zero, new Vector2(820f, 680f));
            var heading = CreateText(result.transform, "ResultHeading", font, "MATCH RESULT", 28, new Color(1f, 0.84f, 0.35f), new Vector2(0f, 280f), new Vector2(700f, 46f), FontStyle.Bold);
            AddShadow(heading);
            var resultTitle = CreateText(result.transform, "ResultTitle", font, "YOU WON", 46, new Color(1f, 0.55f, 0.35f), new Vector2(0f, 220f), new Vector2(700f, 64f), FontStyle.Bold);
            AddShadow(resultTitle);
            CreateText(result.transform, "ColumnNames", font, "PLAYER", 18, new Color(0.75f, 0.9f, 0.92f), new Vector2(-180f, 160f), new Vector2(220f, 30f), FontStyle.Bold);
            CreateText(result.transform, "ColumnPoints", font, "POINTS", 18, new Color(0.75f, 0.9f, 0.92f), new Vector2(220f, 160f), new Vector2(160f, 30f), FontStyle.Bold);
            var resultBody = CreateText(result.transform, "ResultBody", font, "", 22, Color.white, new Vector2(0f, -250f), new Vector2(700f, 30f), FontStyle.Bold);
            for (var i = 0; i < 4; i++)
            {
                var row = CreateImage(result.transform, "ResultRow" + i, panel, new Color(0.08f, 0.28f, 0.36f, 0.95f), new Vector2(0f, 100f - i * 78f), new Vector2(700f, 68f));
                resultRows[i] = (RectTransform)row.transform;
                var portrait = CreateImage(row.transform, "Avatar", avatars[i], Color.white, new Vector2(-300f, 0f), new Vector2(52f, 52f));
                portrait.GetComponent<Image>().preserveAspect = true;
                resultNames[i] = CreateText(row.transform, "Name", font, SeatLabel(i), 26, Color.white, new Vector2(-40f, 0f), new Vector2(280f, 40f), FontStyle.Bold);
                resultNames[i].alignment = TextAnchor.MiddleLeft;
                resultPoints[i] = CreateText(row.transform, "Points", font, "0", 32, new Color(1f, 0.86f, 0.35f), new Vector2(260f, 0f), new Vector2(140f, 44f), FontStyle.Bold);
            }

            var rematchButton = CreateButton(result.transform, "RematchButton", panel, font, "REMATCH", new Vector2(-150f, -280f), new Color(0.95f, 0.62f, 0.15f), out _);
            ((RectTransform)rematchButton.transform).sizeDelta = new Vector2(240f, 64f);
            var menuButton = CreateButton(result.transform, "MenuButton", panel, font, "MAIN MENU", new Vector2(150f, -280f), new Color(0.12f, 0.55f, 0.62f), out _);
            ((RectTransform)menuButton.transform).sizeDelta = new Vector2(240f, 64f);
            result.SetActive(false);

            var libraryObject = new SerializedObject(library);
            libraryObject.FindProperty("cardBack").objectReferenceValue = back;
            var faceProp = libraryObject.FindProperty("faces");
            faceProp.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                faceProp.GetArrayElementAtIndex(i).FindPropertyRelative("cardId").stringValue = entries[i].cardId;
                faceProp.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue = entries[i].sprite;
            }
            libraryObject.ApplyModifiedPropertiesWithoutUndo();

            var tableObject = new SerializedObject(match);
            tableObject.FindProperty("library").objectReferenceValue = library;
            tableObject.FindProperty("deck").objectReferenceValue = deck;
            tableObject.FindProperty("players").objectReferenceValue = playerManager;
            tableObject.FindProperty("arrangement").objectReferenceValue = arrangement;
            tableObject.FindProperty("turnUi").objectReferenceValue = turnUi;
            tableObject.FindProperty("statusLabel").objectReferenceValue = status;
            tableObject.FindProperty("sortLabel").objectReferenceValue = sortLabel;
            tableObject.FindProperty("playLabel").objectReferenceValue = playLabel;
            tableObject.FindProperty("sortButton").objectReferenceValue = sortButton;
            tableObject.FindProperty("playButton").objectReferenceValue = playButton;
            tableObject.FindProperty("autoPlayToggle").objectReferenceValue = autoPlay.GetComponent<Toggle>();
            tableObject.FindProperty("resultPanel").objectReferenceValue = result;
            tableObject.FindProperty("resultTitle").objectReferenceValue = resultTitle;
            tableObject.FindProperty("resultBody").objectReferenceValue = resultBody;
            AssignArray(tableObject.FindProperty("resultNames"), resultNames);
            AssignArray(tableObject.FindProperty("resultPoints"), resultPoints);
            AssignArray(tableObject.FindProperty("resultRows"), resultRows);
            AssignArray(tableObject.FindProperty("handCards"), hand);
            AssignArray(tableObject.FindProperty("opponentBacks"), backs);
            AssignArray(tableObject.FindProperty("tableCards"), tableCards);
            tableObject.FindProperty("playedCards").objectReferenceValue = played;
            AssignArray(tableObject.FindProperty("seatRoots"), new Object[] { bottom, left, top, right });
            AssignArray(tableObject.FindProperty("handRoots"), new Object[] { bottomHand, leftHand, topHand, rightHand });
            AssignArray(tableObject.FindProperty("scoreLabels"), scoreLabels);
            AssignArray(tableObject.FindProperty("decisionVisuals"), decisions);
            AssignArray(tableObject.FindProperty("groupLabels"), new Object[4]);
            AssignArray(tableObject.FindProperty("groupMarkers"), new Object[4]);
            tableObject.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(sortButton.onClick, match.OnSortClicked);
            UnityEventTools.AddPersistentListener(playButton.onClick, match.OnPlayClicked);
            UnityEventTools.AddPersistentListener(rematchButton.onClick, match.Rematch);
            UnityEventTools.AddPersistentListener(menuButton.onClick, match.GoToMenu);

            Directory.CreateDirectory("Assets/Scenes/Game");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Hazari table saved to " + ScenePath);
        }

        [MenuItem("Hazari/Test Hands")]
        public static void TestHands()
        {
            Debug.Log(HandEvaluator.SelfCheck());
            Debug.Log(CardValueCalculator.SelfCheck());
        }

        static void PlaceHand(RectTransform handRoot, CardView[] hand, Dictionary<string, Sprite> faces)
        {
            for (var index = 0; index < hand.Length; index++)
            {
                var id = SampleHand[index];
                Sprite sprite;
                faces.TryGetValue(id, out sprite);
                hand[index] = CreateHandCard(handRoot, "HandCard_" + (index + 1), sprite, SeatFormation.BottomCardSize);
                var rect = (RectTransform)hand[index].transform;
                SeatFormation.Place(rect, index, hand.Length, false, SeatFormation.BottomCardSize, SeatFormation.BottomStep, 0f, false);
                hand[index].SetHome(rect.anchoredPosition, index);
            }
        }

        static CardView CreateHandCard(RectTransform parent, string name, Sprite sprite, Vector2 size)
        {
            var cardObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Card), typeof(CardUI), typeof(CardView), typeof(CanvasGroup));
            cardObject.transform.SetParent(parent, false);
            var rect = (RectTransform)cardObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            var image = cardObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            var view = cardObject.GetComponent<CardView>();
            SetRef(view, "card", cardObject.GetComponent<Card>());
            SetRef(view, "cardUi", cardObject.GetComponent<CardUI>());
            SetRef(view, "rect", rect);
            SetRef(view, "canvasGroup", cardObject.GetComponent<CanvasGroup>());
            SetRef(cardObject.GetComponent<CardUI>(), "faceImage", image);
            return view;
        }

        static void PlaceBacks(RectTransform parent, Image[] backs, Sprite back, int start, bool vertical, Vector2 size, float step, float rotation)
        {
            for (var i = 0; i < 13; i++)
            {
                var image = CreateImage(parent, "Card_" + (i + 1), back, Color.white, Vector2.zero, size).GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                SeatFormation.Place(image.rectTransform, i, 13, vertical, size, step, rotation);
                backs[start + i] = image;
            }
        }

        static void CreateAvatar(Transform parent, Sprite sprite, Vector2 position, float size)
        {
            var portrait = CreateImage(parent, "Avatar", sprite, Color.white, position, new Vector2(size, size));
            var image = portrait.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            var seat = parent.GetComponent<PlayerUI>();
            if (seat == null)
                seat = parent.gameObject.AddComponent<PlayerUI>();
            SetRef(seat, "avatarImage", image);
        }

        static Text CreateName(Transform parent, Font font, string label, int size, Vector2 position, Vector2 bounds)
        {
            var text = CreateText(parent, "PlayerName", font, label, size, new Color(1f, 0.97f, 0.92f), position, bounds, FontStyle.Bold);
            AddShadow(text);
            return text;
        }

        static Text CreateScore(Transform parent, Font font, int size, Vector2 position, Vector2 bounds)
        {
            var text = CreateText(parent, "Score", font, "Score Taken: 0", size, new Color(1f, 0.84f, 0.42f), position, bounds, FontStyle.Bold);
            AddShadow(text);
            return text;
        }

        static GameObject CreateDecision(Transform parent, Font font, Vector2 position)
        {
            var text = CreateText(parent, "DecisionVisual", font, "TURN", 13, new Color(1f, 0.9f, 0.45f), position, new Vector2(92f, 22f), FontStyle.Bold);
            AddShadow(text);
            var cue = text.gameObject.AddComponent<SeatDecisionView>();
            SetRef(cue, "label", text);
            text.gameObject.SetActive(false);
            return text.gameObject;
        }

        static void CreatePlayRow(Transform parent, Image[] tableCards, int start, Vector2 position)
        {
            const float width = 54f;
            const float step = 30f;
            var row = CreateRect(parent, "Played_" + start, position, new Vector2(180f, 90f));
            var origin = -((4 - 1) * step) * 0.5f;
            for (var i = 0; i < 4; i++)
            {
                var imageObject = CreateImage(row, "Card_" + i, null, Color.white, new Vector2(origin + i * step, 0f), new Vector2(width, 78f));
                var image = imageObject.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                imageObject.SetActive(false);
                tableCards[start + i] = image;
            }
        }

        static void AddShadow(Graphic graphic)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(0f, -1f);
            shadow.useGraphicAlpha = true;
        }

        static string SeatLabel(int seat)
        {
            switch (seat)
            {
                case 0: return "You";
                case 1: return "Player 2";
                case 2: return "Player 3";
                default: return "Player 4";
            }
        }

        static GameObject CreateAutoPlay(Transform parent, Sprite sprite, Font font)
        {
            var root = CreateImage(parent, "AutoPlay", sprite, new Color(0.08f, 0.32f, 0.38f, 0.95f), Vector2.zero, new Vector2(210f, 48f));
            var box = CreateImage(root.transform, "Box", sprite, new Color(1f, 0.96f, 0.88f), new Vector2(-70f, 0f), new Vector2(28f, 28f));
            var mark = CreateImage(box.transform, "Check", sprite, new Color(0.1f, 0.55f, 0.42f), Vector2.zero, new Vector2(16f, 16f));
            var toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = mark.GetComponent<Image>();
            toggle.isOn = false;
            var caption = CreateText(root.transform, "Label", font, "Auto Play", 20, Color.white, new Vector2(24f, 0f), new Vector2(130f, 36f), FontStyle.Bold);
            caption.alignment = TextAnchor.MiddleLeft;
            caption.raycastTarget = false;
            return root;
        }

        static Button CreateButton(Transform parent, string name, Sprite sprite, Font font, string label, Vector2 position, Color color, out Text text)
        {
            var buttonObject = CreateImage(parent, name, sprite, color, position, new Vector2(210f, 64f));
            var image = buttonObject.GetComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            text = CreateText(buttonObject.transform, "Label", font, label, 28, Color.white, Vector2.zero, new Vector2(190f, 54f), FontStyle.Bold);
            text.raycastTarget = false;
            return button;
        }

        static GameObject CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = item.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            return item;
        }

        static Text CreateText(Transform parent, string name, Font font, string value, int size, Color color, Vector2 position, Vector2 bounds, FontStyle style)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = bounds;
            rect.anchoredPosition = position;
            var text = item.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            StretchFull(rect);
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        static void AnchorTopStretch(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        static void AnchorTopRight(RectTransform rect, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        static void AnchorTopLeft(RectTransform rect, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        static void AnchorBottomLeft(RectTransform rect, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        static void AnchorBottomRight(RectTransform rect, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        static void SetRef(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static Dictionary<string, Sprite> LoadFaces()
        {
            var faces = new Dictionary<string, Sprite>();
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                {
                    var card = CardData.Create(suit, rank);
                    faces[card.CardId] = LoadSprite("Assets/Cards/" + FileName(suit, rank));
                }
            }

            return faces;
        }

        static string FileName(Suit suit, Rank rank)
        {
            string rankName;
            switch (rank)
            {
                case Rank.Ace: rankName = "ace"; break;
                case Rank.Jack: rankName = "jack"; break;
                case Rank.Queen: rankName = "queen"; break;
                case Rank.King: rankName = "king"; break;
                default: rankName = ((int)rank).ToString(); break;
            }

            return rankName + "_of_" + suit.ToString().ToLowerInvariant() + ".png";
        }

        static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError("Missing sprite " + path);
            return sprite;
        }
    }
}
