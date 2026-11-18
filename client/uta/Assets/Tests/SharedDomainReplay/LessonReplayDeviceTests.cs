using NUnit.Framework;
using UnityEngine;
using Uta.Shared.Domain.Tests.Lesson;

namespace Uta.Client.Tests.SharedDomainReplay
{
    /// <summary>
    /// IL2CPP でビルドしたプレイヤーで再生し、.NET で再生した状態と項目ごとに比べる（lesson-phases.md 8章、build-ci.md 5.2）。
    /// Editor（Mono）では回さない。
    /// </summary>
    public sealed class LessonReplayDeviceTests
    {
        /// <summary>IL2CPP のプレイヤーで走っていること（Mono・Editor で走ったら落とす）。結果の XML の output に platform を残す。</summary>
        [Test]
        public void Player_IsIl2CppPlayer()
        {
            TestContext.WriteLine("platform=" + Application.platform);
#if ENABLE_IL2CPP
            Assert.That(Application.isEditor, Is.False, "Editor で走った");
            Assert.That(
                Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WindowsPlayer,
                Is.True,
                "想定外のプラットフォーム: " + Application.platform);
#else
            Assert.Fail("IL2CPP ではない（Mono または Editor）: " + Application.platform);
#endif
        }

        [Test]
        public void NormalLesson_SameAsDotNet()
        {
            var differences = LessonReplayExpected.Compare(LessonReplayExpected.Normal, LessonReplayScenario.RunNormal());

            Assert.That(differences, Is.Empty, string.Join("\n", differences));
        }

        [Test]
        public void ExamLesson_SameAsDotNet()
        {
            var differences = LessonReplayExpected.Compare(LessonReplayExpected.Exam, LessonReplayScenario.RunExam());

            Assert.That(differences, Is.Empty, string.Join("\n", differences));
        }
    }
}
