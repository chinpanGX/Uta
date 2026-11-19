using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Uta.Client.Tests
{
    /// <summary>起動 → タイトルのページ（サーバーに触らない）。</summary>
    public sealed class BootToTitleTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return TestScenes.DestroyPersistentScopes();
        }

        [UnityTest]
        [Category("Smoke")]
        public IEnumerator Boot_FromBootstrapScene_ReachesTitlePage()
        {
            yield return TestScenes.BootToTitle();
        }
    }
}
