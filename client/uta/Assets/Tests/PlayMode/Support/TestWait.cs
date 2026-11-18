using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Uta.Client.Tests
{
    /// <summary>PlayMode のテストで、条件が成り立つまでフレームを送る。</summary>
    public static class TestWait
    {
        /// <summary>
        /// 毎フレーム failIf → condition の順に見る。failIf が null 以外の文字列を返したら、その文字列で Assert.Fail。
        /// condition が true になったら終わる。timeoutSeconds（Time.realtimeSinceStartup で測る）を過ぎたら
        /// "timeout " + timeoutSeconds + "s: " + description で Assert.Fail。failIf は null でよい。
        /// </summary>
        public static IEnumerator Until(Func<bool> condition, float timeoutSeconds, string description, Func<string> failIf = null)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (true)
            {
                if (failIf != null)
                {
                    var failure = failIf();
                    if (failure != null)
                    {
                        Assert.Fail(failure);
                    }
                }

                if (condition())
                {
                    yield break;
                }

                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("timeout " + timeoutSeconds + "s: " + description);
                }

                yield return null;
            }
        }
    }
}
