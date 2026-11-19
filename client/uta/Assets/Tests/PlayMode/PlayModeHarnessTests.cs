using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Uta.Client.Tests
{
    public sealed class PlayModeHarnessTests
    {
        [UnityTest]
        [Category("Smoke")]
        public IEnumerator Frames_Advance()
        {
            var start = Time.frameCount;
            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            Assert.GreaterOrEqual(Time.frameCount, start + 3);
        }
    }
}
