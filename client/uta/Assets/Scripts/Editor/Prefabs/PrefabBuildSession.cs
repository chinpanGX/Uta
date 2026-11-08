using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Uta.Client.Editor.Prefabs
{
    /// <summary>Prefab を作る間だけ開くプレビューのシーン。PrefabBuildCommands.BuildAll が作り、finally で Dispose する。各 Builder に渡す。</summary>
    public sealed class PrefabBuildSession : IDisposable
    {
        private Scene scene;

        public PrefabBuildSession()
        {
            scene = EditorSceneManager.NewPreviewScene();
        }

        /// <summary>RectTransform 付きの GameObject を作り、このシーンへ移し、Stretch にして scale 1 にする。</summary>
        public GameObject CreateRoot(string name)
        {
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("prefab build session is disposed: " + name);
            }
            var root = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(root, scene);
            PrefabBuildUtil.SetRect((RectTransform)root.transform, RectSpec.Stretch());
            root.transform.localScale = Vector3.one;
            return root;
        }

        public void Dispose()
        {
            if (scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
            scene = default;
        }
    }
}
