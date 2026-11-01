using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceEngineStartTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        private static ProduceStartInput Input(int scenario, int idol, params SupportCardInput[] supports)
        {
            return new ProduceStartInput(scenario, idol, 1, supports);
        }

        private static ProduceState Start(ProduceCatalog catalog, ProduceStartInput input, TestRandomStreamSource streams = null)
        {
            var engine = new ProduceEngine(catalog);
            return engine.Start(input, streams ?? new TestRandomStreamSource(1));
        }

        [Fact]
        public void Start_Idol1NoSupport_InitialState()
        {
            var streams = new TestRandomStreamSource(1);
            var state = Start(Catalog, Input(1, 1), streams);

            Assert.Equal(ProducePhase.WeekSelect, state.Phase);
            Assert.Equal(1, state.Week);
            Assert.Equal(0, state.Attempt);
            Assert.Equal(0, state.NextOperationNumber);
            Assert.Equal(3, state.ExamRetryLeft);
            Assert.Equal(30, state.GetParam(ParamKind.Singing));
            Assert.Equal(20, state.GetParam(ParamKind.Rhythm));
            Assert.Equal(10, state.GetParam(ParamKind.Expression));
            Assert.Equal(30, state.MaxHp);
            Assert.Equal(30, state.Hp);
            Assert.Equal(10, state.NextInstanceId);
            Assert.Equal(
                new[] { (1, 1), (2, 1), (3, 2), (4, 3), (5, 3), (6, 107), (7, 101), (8, 101), (9, 104) },
                state.Deck.Select(c => (c.InstanceId, c.CardId)).ToArray());
            Assert.All(state.Deck, c => Assert.False(c.Enhanced));
            Assert.Equal(50, state.GetBonusPerThousand(ParamKind.Singing));
            Assert.Equal(30, state.GetBonusPerThousand(ParamKind.Rhythm));
            Assert.Equal(20, state.GetBonusPerThousand(ParamKind.Expression));
            Assert.Empty(state.ClassOptionParams);
            Assert.Empty(state.OfferCards);
            Assert.Null(state.Lesson);
            Assert.Null(state.ExamBackup);
            Assert.Null(state.LastExam);
            Assert.Null(state.MidExam);
            Assert.Null(state.Result);
            Assert.Empty(state.Operations);
            Assert.Empty(streams.Requests);
        }

        [Fact]
        public void Start_Idol2_DeckUsesLogicCards()
        {
            var state = Start(Catalog, Input(1, 2));

            Assert.Equal(new[] { 1, 1, 2, 3, 3, 207, 201, 201, 204 }, state.Deck.Select(c => c.CardId).ToArray());
            Assert.Equal(10, state.GetParam(ParamKind.Singing));
            Assert.Equal(30, state.GetParam(ParamKind.Rhythm));
            Assert.Equal(20, state.GetParam(ParamKind.Expression));
        }

        [Fact]
        public void Start_Idol3_DeckSortedWithinGroup()
        {
            var state = Start(Catalog, Input(1, 3));

            Assert.Equal(new[] { 102, 102, 103 }, state.Deck.Skip(state.Deck.Count - 3).Select(c => c.CardId).ToArray());
            Assert.Equal(32, state.MaxHp);
            Assert.Equal(40, state.GetParam(ParamKind.Singing));
            Assert.Equal(30, state.GetParam(ParamKind.Rhythm));
            Assert.Equal(20, state.GetParam(ParamKind.Expression));
        }

        [Fact]
        public void Start_Idol4()
        {
            var state = Start(Catalog, Input(1, 4));

            Assert.Equal(new[] { 203, 203, 206 }, state.Deck.Skip(state.Deck.Count - 3).Select(c => c.CardId).ToArray());
            Assert.Equal(35, state.MaxHp);
        }

        [Fact]
        public void Start_WithSupport6Level1_AddsInitialAndBonus()
        {
            var state = Start(Catalog, Input(1, 1, new SupportCardInput(6, 1, 0)));

            Assert.Equal(38, state.GetParam(ParamKind.Singing));
            Assert.Equal(70, state.GetBonusPerThousand(ParamKind.Singing));
        }

        [Fact]
        public void Start_WithSupport6MaxLevel()
        {
            var state = Start(Catalog, Input(1, 1, new SupportCardInput(6, 50, 4)));

            Assert.Equal(70, state.GetParam(ParamKind.Singing));
            Assert.Equal(110, state.GetBonusPerThousand(ParamKind.Singing));
        }

        [Fact]
        public void Start_WithSupport8Level30()
        {
            var state = Start(Catalog, Input(1, 1, new SupportCardInput(8, 30, 0)));

            Assert.Equal(34, state.MaxHp);
            Assert.Equal(63, state.GetBonusPerThousand(ParamKind.Expression));
            Assert.Equal(34, state.Hp);
        }

        [Fact]
        public void Start_WithSupport4Level21()
        {
            var state = Start(Catalog, Input(1, 1, new SupportCardInput(4, 21, 1)));

            Assert.Equal(32, state.MaxHp);
        }

        [Fact]
        public void Start_ParamCapApplied()
        {
            var rows = TestProduceMaster.LoadRows();
            rows.Scenarios = rows.Scenarios
                .Select(s => s.ScenarioId == 1 ? new ScenarioRow(s.ScenarioId, s.Name, s.Difficulty, s.WeekCount, 35, s.RetryCount) : s)
                .ToArray();
            var catalog = TestProduceMaster.Create(rows);

            var state = Start(catalog, Input(1, 1, new SupportCardInput(6, 50, 4)));

            Assert.Equal(35, state.GetParam(ParamKind.Singing));
            Assert.Equal(20, state.GetParam(ParamKind.Rhythm));
            Assert.Equal(10, state.GetParam(ParamKind.Expression));
        }

        [Fact]
        public void Start_LevelAboveCap_Throws()
        {
            Assert.Throws<ArgumentException>(() => Start(Catalog, Input(1, 1, new SupportCardInput(6, 31, 0))));
        }

        [Fact]
        public void Start_PlanMismatch_Throws()
        {
            Assert.Throws<ArgumentException>(() => Start(Catalog, Input(1, 1, new SupportCardInput(7, 1, 0))));
        }

        [Fact]
        public void Start_DuplicateSupport_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => Start(Catalog, Input(1, 1, new SupportCardInput(1, 1, 0), new SupportCardInput(1, 1, 0))));
        }

        [Fact]
        public void Start_UnknownScenario_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => Start(Catalog, Input(99, 1)));
        }

        [Fact]
        public void Start_Scenario2_Week1_NoRandomDrawn()
        {
            var streams = new TestRandomStreamSource(1);
            var state = Start(Catalog, Input(2, 1), streams);

            Assert.Equal(ProducePhase.WeekSelect, state.Phase);
            Assert.Empty(state.ClassOptionParams);
            Assert.Empty(streams.Requests);
        }
    }
}
