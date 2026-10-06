using System.IO;
using Hazari.UI;
using Hazari.Utilities;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hazari.EditorTools
{
    public static class MainMenuSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";

        [MenuItem("Hazari/Build Main Menu")]
        public static void Build()
        {
            var panel = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.18f, 0.28f);
            camera.orthographic = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasObject = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var root = canvasObject.transform;
            var background = CreateImage(root, "Background", panel, new Color(0.02f, 0.34f, 0.42f), Vector2.zero, new Vector2(1920f, 1080f));
            Stretch((RectTransform)background.transform);
            Blob(root, "GlowGold", new Color(0.98f, 0.62f, 0.12f, 0.55f), new Vector2(620f, 280f), new Vector2(780f, 780f));
            Blob(root, "GlowRose", new Color(0.93f, 0.22f, 0.42f, 0.38f), new Vector2(-680f, -320f), new Vector2(720f, 720f));
            Blob(root, "GlowJade", new Color(0.05f, 0.75f, 0.55f, 0.28f), new Vector2(-520f, 360f), new Vector2(560f, 560f));

            var menu = new GameObject("MainMenu", typeof(RectTransform), typeof(MainMenuUI));
            menu.transform.SetParent(root, false);
            Stretch((RectTransform)menu.transform);
            var ui = menu.GetComponent<MainMenuUI>();

            var home = CreateRect(menu.transform, "Home", Vector2.zero, Vector2.zero);
            Stretch(home);
            var title = CreateText(home, "Title", font, "HAZARI", 92, new Color(1f, 0.86f, 0.28f), new Vector2(0f, 250f), new Vector2(900f, 120f), FontStyle.Bold);
            AddShadow(title);
            var subtitle = CreateText(home, "Subtitle", font, "Choose how to play", 32, new Color(1f, 0.96f, 0.86f), new Vector2(0f, 160f), new Vector2(700f, 50f), FontStyle.Bold);
            AddShadow(subtitle);

            var offline = CreateButton(home, "OfflineButton", panel, font, "OFFLINE", "Play with bots", new Vector2(0f, 40f), new Color(0.95f, 0.62f, 0.12f), new Color(0.35f, 0.16f, 0.02f));
            var online = CreateButton(home, "OnlineButton", panel, font, "ONLINE", "Play with real players", new Vector2(0f, -130f), new Color(0.05f, 0.72f, 0.78f), new Color(0.02f, 0.18f, 0.28f));
            var multiplayer = CreateButton(home, "MultiplayerButton", panel, font, "MULTIPLAYER", "Private room with friends", new Vector2(0f, -300f), new Color(0.93f, 0.28f, 0.48f), Color.white);

            var onlinePanel = CreatePanel(menu.transform, "OnlinePanel", panel, font, "ONLINE", "Sit with players from around the world.", out var onlineStatus);
            var find = CreateButton(onlinePanel.transform, "FindMatchButton", panel, font, "FIND MATCH", "", new Vector2(0f, -40f), new Color(0.05f, 0.72f, 0.78f), new Color(0.02f, 0.16f, 0.24f));
            var onlineBack = CreateButton(onlinePanel.transform, "BackButton", panel, font, "BACK", "", new Vector2(0f, -200f), new Color(0.18f, 0.28f, 0.42f), Color.white);
            onlinePanel.SetActive(false);

            var multiplayerPanel = CreatePanel(menu.transform, "MultiplayerPanel", panel, font, "MULTIPLAYER", "Host a room or join with a code.", out var multiplayerStatus);
            var code = CreateText(multiplayerPanel.transform, "RoomCode", font, "ROOM CODE: ------", 36, new Color(1f, 0.86f, 0.28f), new Vector2(0f, 40f), new Vector2(700f, 56f), FontStyle.Bold);
            AddShadow(code);
            var create = CreateButton(multiplayerPanel.transform, "CreateRoomButton", panel, font, "CREATE ROOM", "", new Vector2(-180f, -80f), new Color(0.95f, 0.62f, 0.12f), new Color(0.35f, 0.16f, 0.02f));
            ((RectTransform)create.transform).sizeDelta = new Vector2(320f, 84f);
            var join = CreateButton(multiplayerPanel.transform, "JoinRoomButton", panel, font, "JOIN ROOM", "", new Vector2(180f, -80f), new Color(0.93f, 0.28f, 0.48f), Color.white);
            ((RectTransform)join.transform).sizeDelta = new Vector2(320f, 84f);
            var input = CreateInput(multiplayerPanel.transform, "RoomCodeInput", panel, font, "ENTER CODE");
            var multiBack = CreateButton(multiplayerPanel.transform, "BackButton", panel, font, "BACK", "", new Vector2(0f, -280f), new Color(0.18f, 0.28f, 0.42f), Color.white);
            multiplayerPanel.SetActive(false);

            var serialized = new SerializedObject(ui);
            serialized.FindProperty("home").objectReferenceValue = home.gameObject;
            serialized.FindProperty("onlinePanel").objectReferenceValue = onlinePanel;
            serialized.FindProperty("multiplayerPanel").objectReferenceValue = multiplayerPanel;
            serialized.FindProperty("onlineStatus").objectReferenceValue = onlineStatus;
            serialized.FindProperty("roomCodeLabel").objectReferenceValue = code;
            serialized.FindProperty("roomCodeInput").objectReferenceValue = input;
            serialized.FindProperty("multiplayerStatus").objectReferenceValue = multiplayerStatus;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(offline.onClick, ui.PlayOffline);
            UnityEventTools.AddPersistentListener(online.onClick, ui.ShowOnline);
            UnityEventTools.AddPersistentListener(multiplayer.onClick, ui.ShowMultiplayer);
            UnityEventTools.AddPersistentListener(find.onClick, ui.FindOnlineMatch);
            UnityEventTools.AddPersistentListener(onlineBack.onClick, ui.ShowHome);
            UnityEventTools.AddPersistentListener(create.onClick, ui.CreatePrivateRoom);
            UnityEventTools.AddPersistentListener(join.onClick, ui.JoinPrivateRoom);
            UnityEventTools.AddPersistentListener(multiBack.onClick, ui.ShowHome);

            Directory.CreateDirectory("Assets/Scenes/MainMenu");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var menuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/Game/Game.unity", true)
            };
            EditorSceneManager.playModeStartScene = menuAsset;
            AssetDatabase.SaveAssets();
            Debug.Log("Hazari main menu saved to " + ScenePath);
        }

        static GameObject CreatePanel(Transform parent, string name, Sprite sprite, Font font, string heading, string body, out Text status)
        {
            var panel = CreateImage(parent, name, sprite, new Color(0.05f, 0.16f, 0.28f, 0.94f), Vector2.zero, new Vector2(860f, 620f));
            var title = CreateText(panel.transform, "Heading", font, heading, 54, new Color(1f, 0.86f, 0.28f), new Vector2(0f, 220f), new Vector2(720f, 80f), FontStyle.Bold);
            AddShadow(title);
            status = CreateText(panel.transform, "Status", font, body, 26, new Color(1f, 0.96f, 0.88f), new Vector2(0f, 140f), new Vector2(720f, 90f), FontStyle.Bold);
            return panel;
        }

        static InputField CreateInput(Transform parent, string name, Sprite sprite, Font font, string placeholder)
        {
            var box = CreateImage(parent, name, sprite, new Color(1f, 0.97f, 0.9f), new Vector2(0f, -180f), new Vector2(420f, 72f));
            var text = CreateText(box.transform, "Text", font, "", 32, new Color(0.12f, 0.16f, 0.22f), Vector2.zero, new Vector2(380f, 60f), FontStyle.Bold);
            text.supportRichText = false;
            var hint = CreateText(box.transform, "Placeholder", font, placeholder, 28, new Color(0.45f, 0.4f, 0.32f), Vector2.zero, new Vector2(380f, 60f), FontStyle.Italic);
            var input = box.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = hint;
            input.characterLimit = 8;
            return input;
        }

        static Button CreateButton(Transform parent, string name, Sprite sprite, Font font, string label, string caption, Vector2 position, Color color, Color textColor)
        {
            var buttonObject = CreateImage(parent, name, sprite, color, position, new Vector2(560f, 120f));
            var image = buttonObject.GetComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.96f, 0.82f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            button.colors = colors;
            var title = CreateText(buttonObject.transform, "Label", font, label, 40, textColor, string.IsNullOrEmpty(caption) ? Vector2.zero : new Vector2(0f, 16f), new Vector2(500f, 56f), FontStyle.Bold);
            title.raycastTarget = false;
            if (!string.IsNullOrEmpty(caption))
            {
                var sub = CreateText(buttonObject.transform, "Caption", font, caption, 22, textColor, new Vector2(0f, -28f), new Vector2(500f, 36f), FontStyle.Bold);
                sub.raycastTarget = false;
            }

            return button;
        }

        static void Blob(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            var blob = CreateImage(parent, name, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"), color, position, size);
            blob.GetComponent<Image>().raycastTarget = false;
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void AddShadow(Graphic graphic)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -2f);
        }
    }
}
