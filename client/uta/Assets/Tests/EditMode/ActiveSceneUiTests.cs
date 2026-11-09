using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Object = UnityEngine.Object;

namespace Uta.Client.Tests
{
    public sealed class ActiveSceneUiTests
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

        /// <summary>EditMode では Awake が走らない（EnsureConfigured も走らない）。ほかの参照は null のまま。</summary>
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
        public void Attach_SetsRootDialogsAndBusy()
        {
            var root = CreateRoot(out var busy);
            var dialogs = new DialogOpener(null);
            var active = new ActiveSceneUi();

            active.Attach(root, dialogs);

            Assert.AreSame(root, active.Root);
            Assert.AreSame(dialogs, active.Dialogs);
            Assert.AreSame(busy, active.Busy);
        }

        [Test]
        public void Detach_SameRoot_Clears()
        {
            var root = CreateRoot(out _);
            var active = new ActiveSceneUi();
            active.Attach(root, new DialogOpener(null));

            active.Detach(root);

            Assert.IsNull(active.Root);
            Assert.IsNull(active.Dialogs);
            Assert.IsNull(active.Busy);
        }

        [Test]
        public void Detach_OtherRoot_KeepsCurrent()
        {
            var a = CreateRoot(out _);
            var b = CreateRoot(out _);
            var active = new ActiveSceneUi();
            active.Attach(a, new DialogOpener(null));
            active.Attach(b, new DialogOpener(null));

            active.Detach(a);

            Assert.AreSame(b, active.Root);
        }

        [Test]
        public void Attach_Null_Throws()
        {
            var root = CreateRoot(out _);
            var active = new ActiveSceneUi();

            Assert.Throws<ArgumentNullException>(() => active.Attach(null, new DialogOpener(null)));
            Assert.Throws<ArgumentNullException>(() => active.Attach(root, null));
        }

        [Test]
        public void Busy_DestroyedRoot_ReturnsNull()
        {
            var root = CreateRoot(out _);
            var active = new ActiveSceneUi();
            active.Attach(root, new DialogOpener(null));

            Object.DestroyImmediate(root.gameObject);

            Assert.IsNull(active.Busy);
        }
    }
}
