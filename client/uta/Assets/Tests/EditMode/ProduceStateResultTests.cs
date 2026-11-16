using System;
using System.Collections.Generic;
using NUnit.Framework;
using Uta.Client.Application.Produce;
using Uta.Shared.Domain.Lesson;

namespace Uta.Client.Tests
{
    public sealed class ProduceStateResultTests
    {
        private static ProduceLesson Lesson(LessonKind kind)
        {
            return new ProduceLesson(kind, ParamKind.Singing, 6, 0, 0, new List<ProduceExamTurn>(), 1, 0, 0, 0,
                new List<int>(), new List<bool>(), new List<int>(), new List<int>(), new List<int>(),
                new List<int>(), new List<ProduceParamBonus>(), new List<ProducePersistent>(), new List<int>(), new List<int>());
        }

        [Test]
        public void GetParam_ReturnsValues()
        {
            var result = TestProduce.StateResult(singing: 110, rhythm: 120, expression: 130);

            Assert.AreEqual(110, result.GetParam(ParamKind.Singing));
            Assert.AreEqual(120, result.GetParam(ParamKind.Rhythm));
            Assert.AreEqual(130, result.GetParam(ParamKind.Expression));
        }

        [Test]
        public void GetParam_Nothing_Throws()
        {
            var result = TestProduce.StateResult();

            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetParam(ParamKind.Nothing));
        }

        [Test]
        public void Lesson_IsExam_ByKind()
        {
            Assert.IsTrue(Lesson(LessonKind.MidExam).IsExam);
            Assert.IsTrue(Lesson(LessonKind.FinalExam).IsExam);
            Assert.IsFalse(Lesson(LessonKind.Normal).IsExam);
        }

        [Test]
        public void Constructor_NullArray_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProduceStateResult(0, Uta.Shared.Domain.Produce.ProducePhase.WeekSelect, 1, 1, 1, 0, 0, 0, 0, 0, 0,
                null, new List<ProduceWeekAction>(), new List<ProduceClassOption>(), new List<int>(), null, null, null));
        }
    }
}
