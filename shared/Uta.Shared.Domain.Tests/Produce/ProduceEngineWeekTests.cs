using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceEngineWeekTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        private static ProduceState Start(ProduceEngine engine, TestRandomStreamSource streams, int scenario)
        {
            return engine.Start(new ProduceStartInput(scenario, 1, 1, new SupportCardInput[0]), streams);
        }

        private static void SkipLesson(ProduceEngine engine, ProduceState state, TestRandomStreamSource streams)
        {
            while (state.Phase == ProducePhase.Lesson)
            {
                Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.Skip(), streams));
            }
        }

        // シナリオ 2 の週 2 の入力待ちまで進める
        private static ProduceState ToScenario2Week2(ProduceEngine engine, TestRandomStreamSource streams)
        {
            var state = Start(engine, streams, 2);
            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams));
            SkipLesson(engine, state, streams);
            Assert.Equal(2, state.Week);
            Assert.Equal(ProducePhase.WeekSelect, state.Phase);
            return state;
        }

        private static void AssertRejected(ProduceEngine engine, ProduceState state, TestRandomStreamSource streams, ProduceCommand command, ProduceApplyResult expected)
        {
            var before = ProduceStateComparer.Describe(state);
            var operationBefore = state.NextOperationNumber;
            var recordsBefore = state.Operations.Count;

            Assert.Equal(expected, engine.Apply(state, command, streams));

            Assert.Equal(before, ProduceStateComparer.Describe(state));
            Assert.Equal(operationBefore, state.NextOperationNumber);
            Assert.Equal(recordsBefore, state.Operations.Count);
        }

        [Fact]
        public void GetWeekChoices_Scenario1Week1_LessonAndUnselectableRest()
        {
            var engine = new ProduceEngine(Catalog);
            var state = Start(engine, new TestRandomStreamSource(1), 1);

            var choices = engine.GetWeekChoices(state);

            Assert.Equal(2, choices.Count);
            Assert.Equal(0, choices[0].SlotIndex);
            Assert.Equal(WeekChoiceKind.Lesson, choices[0].Kind);
            Assert.True(choices[0].Selectable);
            Assert.Equal(1, choices[1].SlotIndex);
            Assert.Equal(WeekChoiceKind.Rest, choices[1].Kind);
            Assert.False(choices[1].Selectable);
        }

        [Fact]
        public void GetWeekChoices_NotWeekSelect_Throws()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams, 1);
            engine.Apply(state, ProduceCommand.ChooseLesson(0, ParamKind.Singing), streams);

            Assert.Equal(ProducePhase.Lesson, state.Phase);
            Assert.Throws<InvalidOperationException>(() => engine.GetWeekChoices(state));
            Assert.Throws<InvalidOperationException>(() => engine.GetClassOptions(state));
        }

        [Fact]
        public void Rest_AtFullHp_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams, 1);

            AssertRejected(engine, state, streams, ProduceCommand.ChooseOther(1), ProduceApplyResult.InvalidArgument);
            Assert.Equal(0, state.NextOperationNumber);
            Assert.Empty(state.Operations);
        }

        [Fact]
        public void Choose_SlotOutOfRange_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams, 1);

            AssertRejected(engine, state, streams, ProduceCommand.ChooseLesson(5, ParamKind.Singing), ProduceApplyResult.InvalidArgument);
            AssertRejected(engine, state, streams, ProduceCommand.ChooseLesson(-1, ParamKind.Singing), ProduceApplyResult.InvalidArgument);
        }

        [Fact]
        public void ChooseLesson_ParamNothing_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams, 1);

            AssertRejected(engine, state, streams, ProduceCommand.ChooseLesson(0, ParamKind.Nothing), ProduceApplyResult.InvalidArgument);
        }

        [Fact]
        public void ChooseLesson_Valid_StartsLesson()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams, 1);
            var command = ProduceCommand.ChooseLesson(0, ParamKind.Singing);

            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, command, streams));

            Assert.Equal(ProducePhase.Lesson, state.Phase);
            Assert.NotNull(state.Lesson);
            Assert.Equal(LessonKind.Normal, state.Lesson.Setup.Kind);
            Assert.Equal(ParamKind.Singing, state.Lesson.Setup.TargetParam);
            Assert.Equal(5, state.Lesson.Setup.MaxTurns);
            Assert.Equal(25, state.Lesson.Setup.ClearTarget);
            Assert.Equal(50, state.Lesson.Setup.PerfectTarget);
            Assert.Equal(state.Hp, state.Lesson.Hp);
            Assert.Single(state.Operations);
            var record = state.Operations[0];
            Assert.Equal(0, record.Number);
            Assert.Equal(1, record.Week);
            Assert.Equal(0, record.Attempt);
            Assert.Equal(0, record.Turn);
            Assert.Equal(command.Kind, record.Command.Kind);
            Assert.Equal(command.Arg1, record.Command.Arg1);
            Assert.Equal(command.Arg2, record.Command.Arg2);
            Assert.Equal(command.Arg3, record.Command.Arg3);
            Assert.Equal(1, state.NextOperationNumber);
            Assert.Contains(new Uta.Shared.Domain.Rng.RandomStreamKey(Uta.Shared.Domain.Rng.RandomStreamKind.Lesson, 1, 0), streams.Requests);
        }

        [Fact]
        public void Choose_WrongPhase_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams, 1);

            AssertRejected(engine, state, streams, ProduceCommand.OfferPick(0), ProduceApplyResult.UnexpectedPhase);
            AssertRejected(engine, state, streams, ProduceCommand.ExamRetry(), ProduceApplyResult.UnexpectedPhase);
            AssertRejected(engine, state, streams, ProduceCommand.UseCard(1), ProduceApplyResult.UnexpectedPhase);
            AssertRejected(engine, state, streams, ProduceCommand.Skip(), ProduceApplyResult.UnexpectedPhase);
        }

        [Fact]
        public void Choose_UnsupportedKind_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = Start(engine, streams, 1);

            AssertRejected(engine, state, streams, new ProduceCommand(OperationKind.DiscardDrink, 0, 0, 0), ProduceApplyResult.InvalidArgument);
            AssertRejected(engine, state, streams, new ProduceCommand(OperationKind.ConsultFinish, 0, 0, 0), ProduceApplyResult.InvalidArgument);
            AssertRejected(engine, state, streams, new ProduceCommand(OperationKind.Nothing, 0, 0, 0), ProduceApplyResult.InvalidArgument);
        }

        [Fact]
        public void Class_Scenario2Week2_Options()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);

            var choices = engine.GetWeekChoices(state);
            Assert.Equal(
                new[] { (0, WeekChoiceKind.Class, true), (1, WeekChoiceKind.Supply, true), (2, WeekChoiceKind.Rest, false) },
                choices.Select(c => (c.SlotIndex, c.Kind, c.Selectable)).ToArray());
            Assert.Equal(3, state.ClassOptionParams.Count);
            Assert.All(state.ClassOptionParams, p => Assert.InRange(p, 1, 3));

            var options = engine.GetClassOptions(state);
            Assert.Equal(new[] { 8, 4, 0 }, options.Select(o => o.HpCost).ToArray());
            Assert.Equal(new[] { 40, 30, 15 }, options.Select(o => o.ParamGain).ToArray());
            Assert.All(options, o => Assert.True(o.Selectable));
            Assert.Equal(state.ClassOptionParams.ToArray(), options.Select(o => (int)o.Param).ToArray());
            Assert.Equal(1, streams.Requests.Count(k => k.Kind == Uta.Shared.Domain.Rng.RandomStreamKind.Week && k.Week == 2 && k.Attempt == 0));
        }

        [Fact]
        public void Class_Option0_ChangesHpAndParamAndOffersCard()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);
            var param = (ParamKind)state.ClassOptionParams[0];
            var before = state.GetParam(param);

            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.ChooseClass(0, 0), streams));

            Assert.Equal(22, state.Hp);
            Assert.Equal(Math.Min(1000, before + 40), state.GetParam(param));
            Assert.Equal(ProducePhase.CardOffer, state.Phase);
            Assert.Equal(3, state.OfferCards.Count);
            Assert.Equal(3, state.OfferCards.Distinct().Count());
            Assert.Empty(state.ClassOptionParams);
        }

        [Fact]
        public void Class_Option2_NoCost()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);
            var param = (ParamKind)state.ClassOptionParams[2];
            var before = state.GetParam(param);

            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.ChooseClass(0, 2), streams));

            Assert.Equal(30, state.Hp);
            Assert.Equal(Math.Min(1000, before + 15), state.GetParam(param));
        }

        [Fact]
        public void Class_UnselectableOption_Rejected()
        {
            var rows = TestProduceMaster.LoadRows();
            rows.ClassOptions = rows.ClassOptions
                .Select(o => o.ScenarioId == 2 && o.Week == 2 && o.Order == 1
                    ? new ClassOptionRow(o.ScenarioId, o.Week, o.Order, 30, o.ParamGain, o.GivesCard)
                    : o)
                .ToArray();
            var catalog = TestProduceMaster.Create(rows);
            var engine = new ProduceEngine(catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);

            var options = engine.GetClassOptions(state);
            Assert.False(options[0].Selectable);
            Assert.True(options[1].Selectable);
            Assert.True(options[2].Selectable);
            AssertRejected(engine, state, streams, ProduceCommand.ChooseClass(0, 0), ProduceApplyResult.InvalidArgument);
            AssertRejected(engine, state, streams, ProduceCommand.ChooseClass(0, 3), ProduceApplyResult.InvalidArgument);
        }

        [Fact]
        public void Offer_Pick_AddsCardAndEndsWeek()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseClass(0, 0), streams);
            var picked = state.OfferCards[1];

            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.OfferPick(1), streams));

            Assert.Equal(10, state.Deck.Count);
            var last = state.Deck[state.Deck.Count - 1];
            Assert.Equal(10, last.InstanceId);
            Assert.Equal(picked, last.CardId);
            Assert.False(last.Enhanced);
            Assert.Equal(11, state.NextInstanceId);
            Assert.Empty(state.OfferCards);
            Assert.Equal(3, state.Week);
            Assert.Equal(ProducePhase.WeekSelect, state.Phase);
            Assert.Equal(3, state.ClassOptionParams.Count);
        }

        [Fact]
        public void Offer_Decline_EndsWeekWithoutCard()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseClass(0, 0), streams);

            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.OfferDecline(), streams));

            Assert.Equal(9, state.Deck.Count);
            Assert.Equal(3, state.Week);
        }

        [Fact]
        public void Offer_PickOutOfRange_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseClass(0, 0), streams);

            AssertRejected(engine, state, streams, ProduceCommand.OfferPick(3), ProduceApplyResult.InvalidArgument);
            AssertRejected(engine, state, streams, ProduceCommand.OfferPick(-1), ProduceApplyResult.InvalidArgument);
        }

        [Fact]
        public void Offer_RerollAndRemove_Rejected()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseClass(0, 0), streams);

            AssertRejected(engine, state, streams, ProduceCommand.OfferReroll(), ProduceApplyResult.InvalidArgument);
            AssertRejected(engine, state, streams, ProduceCommand.OfferRemove(0), ProduceApplyResult.InvalidArgument);
        }

        [Fact]
        public void Supply_OffersCards()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);

            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.ChooseOther(1), streams));

            Assert.Equal(ProducePhase.CardOffer, state.Phase);
            Assert.Equal(3, state.OfferCards.Count);
            Assert.Equal(30, state.Hp);
            Assert.Contains(new Uta.Shared.Domain.Rng.RandomStreamKey(Uta.Shared.Domain.Rng.RandomStreamKind.Offer, 2, 0), streams.Requests);
        }

        [Fact]
        public void Rest_RecoversSeventyPercent()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = ToScenario2Week2(engine, streams);
            engine.Apply(state, ProduceCommand.ChooseClass(0, 0), streams);
            Assert.Equal(22, state.Hp);
            engine.Apply(state, ProduceCommand.OfferDecline(), streams);

            var choices = engine.GetWeekChoices(state);
            Assert.Equal(
                new[] { WeekChoiceKind.Lesson, WeekChoiceKind.Class, WeekChoiceKind.Rest },
                choices.Select(c => c.Kind).ToArray());
            Assert.True(choices[2].Selectable);

            Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, ProduceCommand.ChooseOther(2), streams));

            Assert.Equal(30, state.Hp);
            Assert.Equal(4, state.Week);
            Assert.Equal(ProducePhase.WeekSelect, state.Phase);
        }
    }
}
