using System.Collections.Generic;
using Uta.Client.Bootstrap;
using Uta.Client.Presentation;
using Uta.Client.Presentation.Home;
using Uta.Client.Presentation.Produce;
using Uta.Client.Presentation.Title;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Uta.Client.Editor
{
    /// <summary>Bootstrap / Title / Home / Produce の 4 シーンを空のシーンから作り、Build Settings に並べる。</summary>
    public static class SceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";

        [MenuItem("Uta/Create Scenes")]
        public static void CreateScenes()
        {
            CreateScene("Bootstrap", BuildBootstrap);
            CreateScene("Title", BuildTitle);
            CreateScene("Home", BuildHome);
            CreateScene("Produce", BuildProduce);

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var name in new[] { "Bootstrap", "Title", "Home", "Produce" })
            {
                scenes.Add(new EditorBuildSettingsScene(ScenesFolder + "/" + name + ".unity", true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.DeleteAsset(ScenesFolder + "/SampleScene.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateScene(string name, System.Action<Scene> build)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenesFolder + "/" + name + ".unity");
        }

        private static void BuildBootstrap(Scene scene)
        {
            CreateCamera();
            var entry = new GameObject("BootstrapEntry");
            entry.AddComponent<BootstrapEntry>();
        }

        private static void BuildTitle(Scene scene)
        {
            CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas();
            var start = CreateButton(canvas.transform, "StartButton", "TAP TO START", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 160));
            var status = CreateText(canvas.transform, "StatusText", string.Empty, 36, TextAnchor.UpperCenter, new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(1000, 300));
            var view = canvas.gameObject.AddComponent<TitleView>();
            var so = new SerializedObject(view);
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("TitleScope").AddComponent<TitleLifetimeScope>();
        }

        private static void BuildHome(Scene scene)
        {
            CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas();
            var summary = CreateText(canvas.transform, "SummaryText", string.Empty, 44, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1000, 800));
            var produce = CreateButton(canvas.transform, "ProduceButton", "PRODUCE", new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(600, 160));
            var view = canvas.gameObject.AddComponent<HomeView>();
            var so = new SerializedObject(view);
            so.FindProperty("summaryText").objectReferenceValue = summary;
            so.FindProperty("produceButton").objectReferenceValue = produce;
            so.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("HomeScope").AddComponent<HomeLifetimeScope>();
        }

        private static void BuildProduce(Scene scene)
        {
            CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas();
            CreateText(canvas.transform, "Label", "PRODUCE", 64, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -200), new Vector2(1000, 160));
            var home = CreateButton(canvas.transform, "HomeButton", "BACK TO HOME", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 160));
            var view = canvas.gameObject.AddComponent<ProduceView>();
            var so = new SerializedObject(view);
            so.FindProperty("homeButton").objectReferenceValue = home;
            so.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("ProduceScope").AddComponent<ProduceLifetimeScope>();
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera));
            go.tag = "MainCamera";
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = CreateRect(parent, name, anchor, position, size);
            return ConfigureText(rect.gameObject.AddComponent<Text>(), content, fontSize, alignment);
        }

        private static Text ConfigureText(Text text, string content, int fontSize, TextAnchor alignment)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.supportRichText = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = CreateRect(parent, name, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.35f, 0.6f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            ConfigureText(textGo.AddComponent<Text>(), label, 48, TextAnchor.MiddleCenter);
            return button;
        }
    }
}
