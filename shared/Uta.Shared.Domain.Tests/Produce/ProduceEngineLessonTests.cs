using System;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceEngineLessonTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        private static ProduceState Start(ProduceEngine engine, TestRandomStreamSource streams, int scenario = 1, int idol = 1)
        {
            return engine.Start(new ProduceStartInput(scenario, idol, 1, new SupportCardInput[0]), streams);
        }

        [Fact]
        public void Lesson_SkipOnly_EndsAfterMaxTurnsWithoutReward()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams);

            for (var i = 0; i < 5; i++)
            {
                Assert.Equal(ProducePhase.Lesson, state.Phase);
                Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.Skip(), streams));
            }

            Assert.Equal(ProducePhase.WeekSelect, state.Phase);
            Assert.Equal(2, state.Week);
            Assert.Null(state.Lesson);
            Assert.Equal(30, state.GetParam(ParamKind.Singing));
            Assert.Equal(20, state.GetParam(ParamKind.Rhythm));
            Assert.Equal(10, state.GetParam(ParamKind.Expression));
            Assert.Equal(30, state.Hp);
            Assert.Empty(state.OfferCards);
            Assert.Equal(6, state.Operations.Count);
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, state.Operations.Select(o => o.Turn).ToArray());

            var choices = engine.GetWeekChoices(state);
            Assert.Single(choices);
            Assert.Equal(0, choices[0].SlotIndex);
            Assert.Equal(WeekChoiceKind.Lesson, choices[0].Kind);
            Assert.True(choices[0].Selectable);
        }

        [Fact]
        public void Lesson_Finishing_ChoiceIsSlotZeroOnly()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams);
            while (state.Phase == ProducePhase.Lesson)
            {
                engine.Apply(state, ProduceCommand.Skip(), streams);
            }

            Assert.Equal(2, state.Week);
            Assert.Equal(ProduceApplyResult.InvalidArgument, engine.Apply(state, ProduceCommand.ChooseLesson(1, ParamKind.Singing), streams));
            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams));

            Assert.Equal(LessonKind.Finishing, state.Lesson.Setup.Kind);
            Assert.Equal(9, state.Lesson.Setup.MaxTurns);
            Assert.Equal(75, state.Lesson.Setup.ClearTarget);
            Assert.Equal(300, state.Lesson.Setup.PerfectTarget);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        [InlineData(4, 1)]
        [InlineData(5, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(3, 2)]
        [InlineData(4, 2)]
        [InlineData(5, 2)]
        public void Lesson_BotPlay_ResultAppliedAsSpecified(int seed, int idolId)
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource((ulong)seed);
            var state = Start(engine, streams, 1, idolId);
            var target = idolId == 1 ? ParamKind.Singing : ParamKind.Rhythm;
            var paramBefore = new[]
            {
                state.GetParam(ParamKind.Singing),
                state.GetParam(ParamKind.Rhythm),
                state.GetParam(ParamKind.Expression),
            };
            var bonus = state.GetBonusPerThousand(target);

            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.ChooseLesson(0, target), streams));
            var lesson = state.Lesson;
            while (state.Phase == ProducePhase.Lesson)
            {
                var command = ProduceBot.Next(engine, Catalog, state, true);
                Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, command, streams));
            }

            var result = LessonEngine.GetResult(lesson);
            var targetIndex = ProduceParams.ToIndex(target);
            for (var i = 0; i < 3; i++)
            {
                var expected = paramBefore[i];
                if (i == targetIndex)
                {
                    expected = Math.Min(1000, expected + result.Gained + ProduceFormulas.BonusAmount(result.Gained, bonus));
                }

                Assert.Equal(expected, state.GetParam(ProduceParams.FromIndex(i)));
            }

            var expectedHp = result.Hp;
            if (result.Reason == LessonEndReason.Perfect)
            {
                expectedHp = ProduceFormulas.Heal(
                    result.Hp, ProduceFormulas.PerfectRecover(lesson.Setup.MaxTurns, result.LastTurn), state.MaxHp);
            }

            Assert.Equal(expectedHp, state.Hp);
            if (result.Gained >= 25)
            {
                Assert.Equal(ProducePhase.CardOffer, state.Phase);
                Assert.InRange(state.OfferCards.Count, 1, 3);
            }
            else
            {
                Assert.Equal(2, state.Week);
            }
        }

        [Fact]
        public void Lesson_Command_UseDrink_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams);
            var before = ProduceStateComparer.Describe(state);

            Assert.Equal(ProduceApplyResult.InvalidArgument, engine.Apply(state, ProduceCommand.UseDrink(0), streams));

            Assert.Equal(before, ProduceStateComparer.Describe(state));
        }

        [Fact]
        public void Lesson_Command_InvalidCard_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams);
            var before = ProduceStateComparer.Describe(state);

            Assert.Equal(ProduceApplyResult.InvalidArgument, engine.Apply(state, ProduceCommand.UseCard(9999), streams));

            Assert.Equal(before, ProduceStateComparer.Describe(state));
        }

        [Fact]
        public void Lesson_Command_ChooseWeekInLesson_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams);

            Assert.Equal(ProduceApplyResult.UnexpectedPhase, engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams));
        }

        [Fact]
        public void Lesson_ClearReward_OfferCardsMatchPool()
        {
            var engine = new ProduceEngine(Catalog);
            var idol = Catalog.GetIdol(1);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var streams = new TestRandomStreamSource(seed);
                var state = Start(engine, streams);
                var deckCardIds = state.Deck.Select(c => c.CardId).ToArray();
                engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams);
                while (state.Phase == ProducePhase.Lesson)
                {
                    engine.Apply(state, ProduceBot.Next(engine, Catalog, state, true), streams);
                }

                if (state.Phase != ProducePhase.CardOffer)
                {
                    continue;
                }

                var candidates = OfferPool.GetCandidates(Catalog, idol, 1, deckCardIds);
                foreach (var cardId in state.OfferCards)
                {
                    var card = Catalog.Cards.Get(cardId);
                    Assert.True(card.Plan == Plan.Sense || card.Plan == Plan.Free);
                    Assert.NotEqual(CardType.Trouble, card.Type);
                    Assert.Contains(cardId, candidates);
                }
            }
        }
    }
}
