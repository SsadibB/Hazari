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
            camera.backgroundColor = new Color(0.08f, 0.05f, 0.16f);
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
            var background = CreateImage(root, "Background", panel, new Color(0.10f, 0.05f, 0.20f), Vector2.zero, new Vector2(1920f, 1080f));
            StretchFull((RectTransform)background.transform);
            var header = CreateImage(root, "Header", panel, new Color(0.36f, 0.12f, 0.38f), new Vector2(0f, 490f), new Vector2(1920f, 100f));
            AnchorTopStretch((RectTransform)header.transform, 88f);
            var title = CreateText(root, "Title", font, "HAZARI", 64, new Color(1f, 0.84f, 0.35f), new Vector2(-680f, 492f), new Vector2(520f, 80f), FontStyle.Bold);
            AnchorTopLeft((RectTransform)title.transform, new Vector2(28f, -8f), new Vector2(420f, 72f));
            title.alignment = TextAnchor.MiddleLeft;
            CreateImage(root, "Felt", panel, new Color(0.05f, 0.45f, 0.32f), new Vector2(0f, 10f), new Vector2(1680f, 760f));
            CreateImage(root, "FeltInner", panel, new Color(0.09f, 0.55f, 0.36f), new Vector2(0f, 10f), new Vector2(1560f, 640f));

            var table = new GameObject("Table");
            table.transform.SetParent(root, false);
            var tableRect = table.AddComponent<RectTransform>();
            Stretch(tableRect);
            var library = table.AddComponent<CardSpriteLibrary>();
            var deck = table.AddComponent<DeckManager>();
            var playerManager = table.AddComponent<PlayerManager>();
            var arrangement = table.AddComponent<CardArrangementManager>();
            var turnUi = table.AddComponent<TurnUI>();
            var match = table.AddComponent<HazariTable>();

            var entries = new List<CardSpriteLibrary.Entry>();
            foreach (var pair in faces)
                entries.Add(new CardSpriteLibrary.Entry { cardId = pair.Key, sprite = pair.Value });

            var hand = new CardView[13];
            var handRoot = CreateRect(root, "PlayerHand", new Vector2(0f, -365f), new Vector2(1700f, 230f));
            PlaceHand(handRoot, hand, faces);

            var backs = new Image[39];
            PlaceBacks(root, backs, back, 0, new Vector2(-860f, 10f), true);
            PlaceBacks(root, backs, back, 13, new Vector2(0f, 268f), false);
            PlaceBacks(root, backs, back, 26, new Vector2(860f, 10f), true);

            CreateSeat(root, "SeatYou", avatars[0], font, "You", new Vector2(-860f, -250f), new Color(0.95f, 0.72f, 0.22f));
            CreateSeat(root, "SeatWest", avatars[1], font, "Player 2", new Vector2(-860f, 250f), new Color(0.20f, 0.75f, 0.95f));
            CreateSeat(root, "SeatNorth", avatars[2], font, "Player 3", new Vector2(0f, 392f), new Color(1f, 0.38f, 0.55f));
            CreateSeat(root, "SeatEast", avatars[3], font, "Player 4", new Vector2(860f, 250f), new Color(0.72f, 0.48f, 1f));

            var scoreLabels = new Text[4];
            scoreLabels[0] = CreateText(root, "ScoreYou", font, "0", 24, new Color(0.15f, 0.08f, 0.02f), new Vector2(-860f, -312f), new Vector2(130f, 28f), FontStyle.Bold);
            scoreLabels[1] = CreateText(root, "ScoreWest", font, "0", 24, new Color(0.05f, 0.15f, 0.22f), new Vector2(-860f, 178f), new Vector2(130f, 28f), FontStyle.Bold);
            scoreLabels[2] = CreateText(root, "ScoreNorth", font, "0", 24, new Color(0.35f, 0.05f, 0.12f), new Vector2(0f, 330f), new Vector2(130f, 28f), FontStyle.Bold);
            scoreLabels[3] = CreateText(root, "ScoreEast", font, "0", 24, new Color(0.16f, 0.08f, 0.28f), new Vector2(860f, 178f), new Vector2(130f, 28f), FontStyle.Bold);

            var tableCards = new Image[16];
            CreatePlayRow(root, tableCards, 0, "You", font, new Vector2(0f, -130f), new Color(1f, 0.84f, 0.35f));
            CreatePlayRow(root, tableCards, 4, "Player 2", font, new Vector2(-390f, 40f), new Color(0.35f, 0.9f, 1f));
            CreatePlayRow(root, tableCards, 8, "Player 3", font, new Vector2(0f, 175f), new Color(1f, 0.55f, 0.7f));
            CreatePlayRow(root, tableCards, 12, "Player 4", font, new Vector2(390f, 40f), new Color(0.82f, 0.65f, 1f));

            var status = CreateText(root, "Status", font, "One overlapping row. From the left: 3, 3, 3, then 4.", 26, new Color(1f, 0.95f, 0.8f), new Vector2(0f, -490f), new Vector2(1100f, 50f), FontStyle.Bold);
            AnchorBottomLeft((RectTransform)status.transform, new Vector2(24f, 28f), new Vector2(980f, 50f));
            status.alignment = TextAnchor.MiddleLeft;
            var turn = CreateText(root, "Turn", font, "ARRANGE YOUR HAND", 34, new Color(1f, 0.9f, 0.45f), new Vector2(0f, 95f), new Vector2(700f, 50f), FontStyle.Bold);
            SetRef(turnUi, "label", turn);

            var sortButton = CreateButton(root, "SortButton", panel, font, "SORT", new Vector2(620f, -500f), new Color(0.15f, 0.65f, 0.72f), out var sortLabel);
            var playButton = CreateButton(root, "PlayButton", panel, font, "READY", new Vector2(860f, -500f), new Color(0.9f, 0.35f, 0.38f), out var playLabel);
            AnchorBottomRight((RectTransform)playButton.transform, new Vector2(-24f, 20f), new Vector2(210f, 64f));
            AnchorBottomRight((RectTransform)sortButton.transform, new Vector2(-250f, 20f), new Vector2(210f, 64f));

            var result = CreateImage(root, "ResultPanel", panel, new Color(0.12f, 0.08f, 0.22f, 0.96f), Vector2.zero, new Vector2(760f, 520f));
            CreateText(result.transform, "ResultHeading", font, "MATCH RESULT", 28, new Color(1f, 0.84f, 0.35f), new Vector2(0f, 190f), new Vector2(640f, 50f), FontStyle.Bold);
            var resultTitle = CreateText(result.transform, "ResultTitle", font, "YOU WON", 54, new Color(1f, 0.55f, 0.35f), new Vector2(0f, 110f), new Vector2(640f, 80f), FontStyle.Bold);
            var resultBody = CreateText(result.transform, "ResultBody", font, "", 32, Color.white, new Vector2(0f, -20f), new Vector2(500f, 220f), FontStyle.Bold);
            var rematchButton = CreateButton(result.transform, "RematchButton", panel, font, "REMATCH", new Vector2(0f, -190f), new Color(0.95f, 0.62f, 0.15f), out _);
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
            tableObject.FindProperty("resultPanel").objectReferenceValue = result;
            tableObject.FindProperty("resultTitle").objectReferenceValue = resultTitle;
            tableObject.FindProperty("resultBody").objectReferenceValue = resultBody;
            AssignArray(tableObject.FindProperty("handCards"), hand);
            AssignArray(tableObject.FindProperty("opponentBacks"), backs);
            AssignArray(tableObject.FindProperty("tableCards"), tableCards);
            AssignArray(tableObject.FindProperty("scoreLabels"), scoreLabels);
            AssignArray(tableObject.FindProperty("groupLabels"), new Object[4]);
            AssignArray(tableObject.FindProperty("groupMarkers"), new Object[4]);
            tableObject.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(sortButton.onClick, match.OnSortClicked);
            UnityEventTools.AddPersistentListener(playButton.onClick, match.OnPlayClicked);
            UnityEventTools.AddPersistentListener(rematchButton.onClick, match.Rematch);

            Directory.CreateDirectory("Assets/Scenes/Game");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Hazari table saved to " + ScenePath);
        }

        [MenuItem("Hazari/Test Hands")]
        public static void TestHands()
        {
            Debug.Log(HandEvaluator.SelfCheck());
        }

        static void PlaceHand(RectTransform handRoot, CardView[] hand, Dictionary<string, Sprite> faces)
        {
            const float cardWidth = 104f;
            const float step = 78f;
            var width = cardWidth + (hand.Length - 1) * step;
            var x = -width * 0.5f + cardWidth * 0.5f;
            for (var index = 0; index < hand.Length; index++)
            {
                var id = SampleHand[index];
                Sprite sprite;
                faces.TryGetValue(id, out sprite);
                hand[index] = CreateHandCard(handRoot, "HandCard_" + (index + 1), sprite, new Vector2(x, 24f), new Vector2(cardWidth, 148f));
                hand[index].transform.SetSiblingIndex(index);
                x += step;
            }
        }

        static CardView CreateHandCard(RectTransform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var cardObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Card), typeof(CardUI), typeof(CardView), typeof(CanvasGroup));
            cardObject.transform.SetParent(parent, false);
            var rect = (RectTransform)cardObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
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

        static void PlaceBacks(Transform parent, Image[] backs, Sprite back, int start, Vector2 origin, bool vertical)
        {
            for (var i = 0; i < 13; i++)
            {
                var offset = vertical ? new Vector2(0f, 150f - i * 24f) : new Vector2(-150f + i * 24f, 0f);
                var image = CreateImage(parent, "Back_" + start + "_" + i, back, Color.white, origin + offset, new Vector2(58f, 82f)).GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                backs[start + i] = image;
            }
        }

        static void CreateSeat(Transform parent, string name, Sprite avatar, Font font, string label, Vector2 position, Color plateColor)
        {
            CreateImage(parent, name + "Plate", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), plateColor, position, new Vector2(148f, 156f));
            var portrait = CreateImage(parent, name + "Avatar", avatar, Color.white, position + new Vector2(0f, 22f), new Vector2(96f, 96f));
            portrait.GetComponent<Image>().preserveAspect = true;
            CreateText(parent, name + "Name", font, label, 22, Color.white, position + new Vector2(0f, -52f), new Vector2(150f, 32f), FontStyle.Bold);
        }

        static void CreatePlayRow(Transform parent, Image[] tableCards, int start, string label, Font font, Vector2 position, Color color)
        {
            CreateText(parent, label + "Row", font, label, 20, color, position + new Vector2(-210f, 0f), new Vector2(140f, 36f), FontStyle.Bold);
            for (var i = 0; i < 4; i++)
            {
                var imageObject = CreateImage(parent, label + "Play_" + i, null, Color.white, position + new Vector2(-70f + i * 78f, 0f), new Vector2(72f, 102f));
                var image = imageObject.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                imageObject.SetActive(false);
                tableCards[start + i] = image;
            }
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
