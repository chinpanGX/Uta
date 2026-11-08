using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Uta.Client.Bootstrap;
using Uta.Client.Presentation;

namespace Uta.Client.Tests
{
    /// <summary>SceneBuilder が作ったシーンの参照の契約。uiRoot が欠けると、失敗のダイアログや画面が出ない。</summary>
    public sealed class SceneContractTests
    {
        [Test]
        public void Bootstrap_UiRoot_IsSet()
        {
            AssertUiRootSet<BootstrapEntry>("Assets/Scenes/Bootstrap.unity");
        }

        [Test]
        public void Title_UiRoot_IsSet()
        {
            AssertUiRootSet<TitleLifetimeScope>("Assets/Scenes/Title.unity");
        }

        [Test]
        public void Home_UiRoot_IsSet()
        {
            AssertUiRootSet<HomeLifetimeScope>("Assets/Scenes/Home.unity");
        }

        private static void AssertUiRootSet<T>(string path) where T : Component
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                T target = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    target = root.GetComponentInChildren<T>(true);
                    if (target != null)
                    {
                        break;
                    }
                }

                Assert.IsNotNull(target, typeof(T).Name + " not found in " + path);
                var property = new SerializedObject(target).FindProperty("uiRoot");
                Assert.IsNotNull(property, "uiRoot property not found");
                Assert.IsNotNull(property.objectReferenceValue, "uiRoot is not set in " + path);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
