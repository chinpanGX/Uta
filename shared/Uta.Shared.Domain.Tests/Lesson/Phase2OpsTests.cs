using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Xunit;
using static Uta.Shared.Domain.Tests.Lesson.TestCards;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class Phase2OpsTests
    {
        private const int CardId = 901;

        private static LessonState UseSynthetic(CardFace face, int hp = 30, int shield = 0,
            IDictionary<StatusKind, int> statuses = null, bool active = false)
        {
            var type = active ? CardType.Active : CardType.Mental;
            var card = Card(CardId, Plan.Free, type, Rarity.N, false, false, face, null);
            var engine = new LessonEngine(new TestCardCatalog(new[] { card }));
            var state = TestStates.PlayerState(new[] { CardId }, new[] { 1 }, new int[0], hp,
                usesLeft: 2, shield: shield, statuses: statuses);
            var result = engine.Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());
            Assert.Equal(LessonApplyResult.Accepted, result);
            return state;
        }

        private static Dictionary<StatusKind, int> Statuses(StatusKind kind, int value)
        {
            return new Dictionary<StatusKind, int> { { kind, value } };
        }

        [Fact]
        public void AddFixedShield_IgnoresMotivation()
        {
            var state = UseSynthetic(Face(Consume(0), FixedShield(4)), statuses: Statuses(StatusKind.Motivation, 3));
            Assert.Equal(4, state.Shield);
        }

        [Fact]
        public void AddFixedShield_ShieldGainBlocked_NoChange()
        {
            var state = UseSynthetic(Face(Consume(0), FixedShield(4)), shield: 2, statuses: Statuses(StatusKind.ShieldGainBlock, 2));
            Assert.Equal(2, state.Shield);
        }

        [Fact]
        public void RecoverHp_AddsValue()
        {
            Assert.Equal(15, UseSynthetic(Face(Consume(0), Recover(5)), hp: 10).Hp);
        }

        [Fact]
        public void RecoverHp_CapsAtMaxHp()
        {
            Assert.Equal(30, UseSynthetic(Face(Consume(0), Recover(5)), hp: 28).Hp);
        }

        [Theory]
        [InlineData(50, 3)]
        [InlineData(0, 0)]
        [InlineData(100, 7)]
        public void ScaleShield_Percent_Floors(int percent, int expected)
        {
            Assert.Equal(expected, UseSynthetic(Face(Consume(0), ScaleShield(percent)), shield: 7).Shield);
        }

        [Theory]
        [InlineData(9)]
        [InlineData(0)]
        public void SetShield_ShieldGainBlocked_StillSets(int value)
        {
            var state = UseSynthetic(Face(Consume(0), SetShield(value)), shield: 3, statuses: Statuses(StatusKind.ShieldGainBlock, 2));
            Assert.Equal(value, state.Shield);
        }

        [Fact]
        public void GainParamByShield_WithGoodCondition()
        {
            var statuses = Statuses(StatusKind.GoodCondition, 2);
            var state = UseSynthetic(Face(Consume(0), ParamByShield(130)), shield: 7, statuses: statuses, active: true);
            // ceil(7 * 1.3) = 10、好調倍率 1.5 → 15
            Assert.Equal(15, state.ParamGained);
        }

        [Fact]
        public void GainParamByMotivation_Ratio()
        {
            var state = UseSynthetic(Face(Consume(0), ParamByMotivation(120)), statuses: Statuses(StatusKind.Motivation, 5), active: true);
            Assert.Equal(6, state.ParamGained);
        }

        [Theory]
        [InlineData(CostKind.Impression, StatusKind.Impression)]
        [InlineData(CostKind.Motivation, StatusKind.Motivation)]
        public void Cost_Status_PaysAndGates(CostKind costKind, StatusKind status)
        {
            var card = Card(CardId, Plan.Free, CardType.Mental, Rarity.N, false, false, Face(StatusCost(costKind, 2)), null);
            var engine = new LessonEngine(new TestCardCatalog(new[] { card }));

            var poor = TestStates.PlayerState(new[] { CardId }, new[] { 1 }, new int[0], 30, usesLeft: 2, statuses: Statuses(status, 1));
            Assert.Equal(UseCheckResult.CannotPayCost, engine.CanUse(poor, 1));

            var rich = TestStates.PlayerState(new[] { CardId }, new[] { 1 }, new int[0], 30, usesLeft: 2, statuses: Statuses(status, 3));
            Assert.Equal(UseCheckResult.Ok, engine.CanUse(rich, 1));
            engine.Apply(rich, LessonCommand.UseCard(1), TestStates.NewRandom());
            Assert.Equal(1, rich.Statuses.Get(status));
        }

        [Fact]
        public void DebuffGuard_CancelsCostDouble()
        {
            var state = UseSynthetic(Face(Consume(0), Status(StatusKind.CostDouble, 2)), statuses: Statuses(StatusKind.DebuffGuard, 1));
            Assert.Equal(0, state.Statuses.Get(StatusKind.CostDouble));
            Assert.Equal(0, state.Statuses.Get(StatusKind.DebuffGuard));
        }

        [Fact]
        public void DebuffGuard_NoGuard_AppliesWithSkipMark()
        {
            var state = UseSynthetic(Face(Consume(0), Status(StatusKind.CostDouble, 2)));
            Assert.Equal(2, state.Statuses.Get(StatusKind.CostDouble));
            Assert.True(state.Statuses.IsSkipNextDecrement(StatusKind.CostDouble));
        }

        [Fact]
        public void DebuffGuard_NonDebuffStatus_NotConsumed()
        {
            var state = UseSynthetic(Face(Consume(0), Status(StatusKind.CostHalf, 2)), statuses: Statuses(StatusKind.DebuffGuard, 1));
            Assert.Equal(2, state.Statuses.Get(StatusKind.CostHalf));
            Assert.Equal(1, state.Statuses.Get(StatusKind.DebuffGuard));
        }

        [Fact]
        public void DebuffGuard_GuardItself_Accumulates()
        {
            var state = UseSynthetic(Face(Consume(0), Status(StatusKind.DebuffGuard, 2)));
            Assert.Equal(2, state.Statuses.Get(StatusKind.DebuffGuard));
        }

        [Fact]
        public void DebuffGuard_TwoGuards_CancelTwiceThenApplies()
        {
            var face = Face(Consume(0),
                Status(StatusKind.ShieldGainBlock, 1),
                Status(StatusKind.ShieldGainBlock, 1),
                Status(StatusKind.ShieldGainBlock, 1));
            var state = UseSynthetic(face, statuses: Statuses(StatusKind.DebuffGuard, 2));
            Assert.Equal(1, state.Statuses.Get(StatusKind.ShieldGainBlock));
            Assert.Equal(0, state.Statuses.Get(StatusKind.DebuffGuard));
        }
    }
}
