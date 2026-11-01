using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceCommandTests
    {
        private static void Check(ProduceCommand c, OperationKind kind, int a1, int a2, int a3)
        {
            Assert.Equal(kind, c.Kind);
            Assert.Equal(a1, c.Arg1);
            Assert.Equal(a2, c.Arg2);
            Assert.Equal(a3, c.Arg3);
        }

        [Fact]
        public void Factories_CreateExpectedCommands()
        {
            Check(ProduceCommand.ChooseLesson(2, ParamKind.Rhythm), OperationKind.ChooseWeekAction, 2, -1, (int)ParamKind.Rhythm);
            Check(ProduceCommand.ChooseClass(1, 3), OperationKind.ChooseWeekAction, 1, 3, -1);
            Check(ProduceCommand.ChooseOther(4), OperationKind.ChooseWeekAction, 4, -1, -1);
            Check(ProduceCommand.OfferPick(2), OperationKind.OfferPick, 2, 0, 0);
            Check(ProduceCommand.OfferReroll(), OperationKind.OfferReroll, 0, 0, 0);
            Check(ProduceCommand.OfferRemove(1), OperationKind.OfferRemove, 1, 0, 0);
            Check(ProduceCommand.OfferDecline(), OperationKind.OfferDecline, 0, 0, 0);
            Check(ProduceCommand.ExamRetry(), OperationKind.ExamRetry, 0, 0, 0);
            Check(ProduceCommand.ExamNoRetry(), OperationKind.ExamNoRetry, 0, 0, 0);
            Check(ProduceCommand.UseCard(7), OperationKind.UseCard, 7, 0, 0);
            Check(ProduceCommand.UseDrink(1), OperationKind.UseDrink, 1, 0, 0);
            Check(ProduceCommand.Skip(), OperationKind.Skip, 0, 0, 0);
        }

        [Fact]
        public void OperationRecord_KeepsArguments()
        {
            var command = ProduceCommand.UseCard(5);
            var record = new OperationRecord(3, command, 4, 1, 2);
            Assert.Equal(3, record.Number);
            Assert.Equal(OperationKind.UseCard, record.Command.Kind);
            Assert.Equal(5, record.Command.Arg1);
            Assert.Equal(4, record.Week);
            Assert.Equal(1, record.Attempt);
            Assert.Equal(2, record.Turn);
        }
    }

    public class ProduceInputsTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, -1)]
        [InlineData(1, 5)]
        public void SupportCardInput_OutOfRange_Throws(int level, int limitBreak)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SupportCardInput(1, level, limitBreak));
        }

        [Fact]
        public void SupportCardInput_Valid_KeepsValues()
        {
            var input = new SupportCardInput(9, 30, 4);
            Assert.Equal(9, input.CardId);
            Assert.Equal(30, input.Level);
            Assert.Equal(4, input.LimitBreak);
        }

        private static List<SupportCardInput> Cards(int count)
        {
            var list = new List<SupportCardInput>();
            for (var i = 0; i < count; i++)
            {
                list.Add(new SupportCardInput(i + 1, 1, 0));
            }

            return list;
        }

        [Fact]
        public void ProduceStartInput_NullCards_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProduceStartInput(1, 1, 1, null));
        }

        [Fact]
        public void ProduceStartInput_SixCards_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ProduceStartInput(1, 1, 1, Cards(6)));
        }

        [Fact]
        public void ProduceStartInput_NullElement_Throws()
        {
            var cards = Cards(2);
            cards[1] = null;
            Assert.Throws<ArgumentException>(() => new ProduceStartInput(1, 1, 1, cards));
        }

        [Fact]
        public void ProduceStartInput_PlayerLevelZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProduceStartInput(1, 1, 0, Cards(0)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void ProduceStartInput_ValidCounts_Created(int count)
        {
            var input = new ProduceStartInput(2, 3, 10, Cards(count));
            Assert.Equal(count, input.SupportCards.Count);
            Assert.Equal(2, input.ScenarioId);
            Assert.Equal(3, input.IdolId);
            Assert.Equal(10, input.PlayerLevel);
        }

        [Fact]
        public void ProduceStartInput_CopiesCards()
        {
            var cards = Cards(2);
            var input = new ProduceStartInput(1, 1, 1, cards);
            cards.Clear();
            Assert.Equal(2, input.SupportCards.Count);
        }
    }
}
