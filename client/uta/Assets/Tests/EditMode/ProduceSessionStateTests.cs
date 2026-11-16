using System;
using NUnit.Framework;
using Uta.Client.Application.Produce;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    public sealed class ProduceSessionStateTests
    {
        [Test]
        public void Set_UpdatesCurrentAndPrevious()
        {
            var session = new ProduceSessionState();
            var first = TestProduce.StateResult(week: 1);
            var second = TestProduce.StateResult(week: 2);

            session.Set(first);
            session.Set(second);

            Assert.AreSame(first, session.Previous);
            Assert.AreSame(second, session.Current);
        }

        [Test]
        public void Set_ResetsExamResultShown()
        {
            var session = new ProduceSessionState();
            session.Set(TestProduce.StateResult());
            session.ExamResultShown = true;

            session.Set(TestProduce.StateResult(week: 2));

            Assert.IsFalse(session.ExamResultShown);
        }

        [Test]
        public void Set_Null_Throws()
        {
            var session = new ProduceSessionState();

            Assert.Throws<ArgumentNullException>(() => session.Set(null));
        }

        [Test]
        public void Clear_ResetsAll()
        {
            var session = new ProduceSessionState();
            session.Set(TestProduce.StateResult());
            session.Set(TestProduce.StateResult(week: 2));
            session.ExamResultShown = true;

            session.Clear();

            Assert.IsNull(session.Current);
            Assert.IsNull(session.Previous);
            Assert.IsFalse(session.ExamResultShown);
        }

        [Test]
        public void HasActive_ByPhase()
        {
            var session = new ProduceSessionState();
            Assert.IsFalse(session.HasActive);

            session.Set(TestProduce.StateResult(ProducePhase.Nothing));
            Assert.IsFalse(session.HasActive);

            session.Set(TestProduce.StateResult(ProducePhase.Finished));
            Assert.IsFalse(session.HasActive);

            session.Set(TestProduce.StateResult(ProducePhase.WeekSelect));
            Assert.IsTrue(session.HasActive);
        }
    }
}
