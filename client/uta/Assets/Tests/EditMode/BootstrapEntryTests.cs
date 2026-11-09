using NUnit.Framework;
using UnityEngine;
using Uta.Client.Bootstrap;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Tests
{
    public sealed class BootstrapEntryTests
    {
        [Test]
        public void ShowFailureDialog_DestroyedRoot_ReturnsFalse()
        {
            var root = new GameObject("Root").AddComponent<UiRoot>();
            Object.DestroyImmediate(root.gameObject);

            Assert.IsFalse(BootstrapEntry.ShowFailureDialog(root, "m"));
        }

        [Test]
        public void ShowFailureDialog_Null_ReturnsFalse()
        {
            Assert.IsFalse(BootstrapEntry.ShowFailureDialog(null, "m"));
        }
    }
}
