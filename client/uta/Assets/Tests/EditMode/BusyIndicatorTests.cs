using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Tests
{
    public sealed class BusyIndicatorTests
    {
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }
        }

        private BusyIndicator Create()
        {
            host = new GameObject("Busy");
            return host.AddComponent<BusyIndicator>();
        }

        [Test]
        public void Begin_Once_IsBusy()
        {
            var busy = Create();

            busy.Begin();
            Assert.IsTrue(busy.IsBusy);

            busy.End();
            Assert.IsFalse(busy.IsBusy);
        }

        [Test]
        public void Begin_Nested_StaysBusyUntilAllEnded()
        {
            var busy = Create();

            busy.Begin();
            busy.Begin();
            busy.End();
            Assert.IsTrue(busy.IsBusy);

            busy.End();
            Assert.IsFalse(busy.IsBusy);
        }

        [Test]
        public void End_WithoutBegin_DoesNothing()
        {
            var busy = Create();

            Assert.DoesNotThrow(() => busy.End());
            Assert.IsFalse(busy.IsBusy);

            busy.Begin();
            Assert.IsTrue(busy.IsBusy);
        }

        [Test]
        public void Begin_WithBlocker_EnablesImage()
        {
            var busy = Create();
            var image = new GameObject("Blocker").AddComponent<Image>();
            image.transform.SetParent(host.transform, false);
            image.enabled = false;
            var so = new SerializedObject(busy);
            so.FindProperty("blocker").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();

            busy.Begin();
            Assert.IsTrue(image.enabled);

            busy.End();
            Assert.IsFalse(image.enabled);
        }
    }
}
