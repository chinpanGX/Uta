using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Uta.Client.Bootstrap;
using Uta.Client.Presentation.Common;
using Object = UnityEngine.Object;

namespace Uta.Client.Tests
{
    public sealed class ApiActivityBridgeTests
    {
        private readonly System.Collections.Generic.List<GameObject> created = new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            created.Clear();
        }

        private UiRoot CreateRoot(out BusyIndicator busy)
        {
            var root = new GameObject("Root").AddComponent<UiRoot>();
            busy = new GameObject("Busy").AddComponent<BusyIndicator>();
            created.Add(root.gameObject);
            created.Add(busy.gameObject);
            var so = new SerializedObject(root);
            so.FindProperty("busyIndicator").objectReferenceValue = busy;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        [Test]
        public void Started_BeginsCurrentBusy()
        {
            var root = CreateRoot(out var busy);
            var sceneUi = new ActiveSceneUi();
            sceneUi.Attach(root, new DialogOpener(null, null));
            var bridge = new ApiActivityBridge(sceneUi);

            bridge.OnActivityStarted();
            Assert.IsTrue(busy.IsBusy);

            bridge.OnActivityEnded();
            Assert.IsFalse(busy.IsBusy);
        }

        [Test]
        public void Ended_AfterSceneChange_EndsOriginal()
        {
            var rootA = CreateRoot(out var busyA);
            var rootB = CreateRoot(out var busyB);
            var sceneUi = new ActiveSceneUi();
            sceneUi.Attach(rootA, new DialogOpener(null, null));
            var bridge = new ApiActivityBridge(sceneUi);

            bridge.OnActivityStarted();
            sceneUi.Attach(rootB, new DialogOpener(null, null));
            bridge.OnActivityEnded();

            Assert.IsFalse(busyA.IsBusy);
            Assert.IsFalse(busyB.IsBusy);
        }

        [Test]
        public void NoScene_DoesNothing()
        {
            var bridge = new ApiActivityBridge(new ActiveSceneUi());

            Assert.DoesNotThrow(() =>
            {
                bridge.OnActivityStarted();
                bridge.OnActivityEnded();
            });
        }
    }
}
