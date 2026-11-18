using System.Collections.Generic;
using Uta.Client.Bootstrap;
using Uta.Client.Presentation;
using Uta.Client.Presentation.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Uta.Client.Editor
{
    /// <summary>Bootstrap / Title / Home / Produce の 4 シーンを空のシーンから作り、Build Settings に並べる。</summary>
    public static class SceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string UiRootPrefabPath = "Assets/Prefabs/Parts/UiRoot.prefab";

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
            var uiRoot = InstantiateUiRoot();
            var entry = new GameObject("BootstrapEntry").AddComponent<BootstrapEntry>();
            SetRef(entry, "uiRoot", uiRoot);
        }

        private static void BuildTitle(Scene scene)
        {
            CreateCamera();
            var uiRoot = InstantiateUiRoot();
            var scope = new GameObject("TitleScope").AddComponent<TitleLifetimeScope>();
            SetRef(scope, "uiRoot", uiRoot);
        }

        private static void BuildHome(Scene scene)
        {
            CreateCamera();
            var uiRoot = InstantiateUiRoot();
            var scope = new GameObject("HomeScope").AddComponent<HomeLifetimeScope>();
            SetRef(scope, "uiRoot", uiRoot);
        }

        private static UiRoot InstantiateUiRoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiRootPrefabPath);
            if (prefab == null)
            {
                throw new System.InvalidOperationException("UiRoot prefab not found. run Uta/Prefabs/Build All first");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return instance.GetComponent<UiRoot>();
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                throw new System.InvalidOperationException(target.GetType().Name + "." + field + " not found");
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildProduce(Scene scene)
        {
            CreateCamera();
            var uiRoot = InstantiateUiRoot();
            var scope = new GameObject("ProduceScope").AddComponent<ProduceLifetimeScope>();
            SetRef(scope, "uiRoot", uiRoot);
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera));
            go.tag = "MainCamera";
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
