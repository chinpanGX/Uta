using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class Phase3CardsTests
    {
        private static readonly TestCardCatalog Catalog = new TestCardCatalog();
        private static readonly LessonEngine Engine = new LessonEngine(Catalog);

        private static Dictionary<StatusKind, int> Statuses(params (StatusKind Kind, int Value)[] pairs)
        {
            return pairs.ToDictionary(p => p.Kind, p => p.Value);
        }

        private static LessonState Setup(int[] cardIds, int[] hand, int hp = 30, int shield = 0, int usesLeft = 2,
            IDictionary<StatusKind, int> statuses = null, int[] enhanced = null)
        {
            return TestStates.PlayerState(cardIds, hand, new int[0], hp, usesLeft: usesLeft, shield: shield,
                statuses: statuses, enhanced: enhanced);
        }

        private static void Use(LessonState state, int instanceId)
        {
            var result = Engine.Apply(state, LessonCommand.UseCard(instanceId), TestStates.NewRandom());
            Assert.Equal(LessonApplyResult.Accepted, result);
        }

        private static void Skip(LessonState state)
        {
            var result = Engine.Apply(state, LessonCommand.Skip(), TestStates.NewRandom());
            Assert.Equal(LessonApplyResult.Accepted, result);
        }

        private static int Gained(int cardId, bool enhanced, params (StatusKind Kind, int Value)[] statuses)
        {
            var state = Setup(new[] { cardId }, new[] { 1 }, statuses: Statuses(statuses),
                enhanced: enhanced ? new[] { 1 } : null);
            Use(state, 1);
            return state.ParamGained;
        }

        [Fact]
        public void UseCard_S11_GoodCondition()
        {
            var state = Setup(new[] { Phase3Cards.S11 }, new[] { 1 }, statuses: Statuses((StatusKind.GoodCondition, 2)));
            Assert.Equal(UseCheckResult.Ok, Engine.CanUse(state, 1));
            Use(state, 1);
            Assert.Equal(24, state.Hp);
            Assert.Equal(41, state.ParamGained);
            Assert.Equal(57, Gained(Phase3Cards.S11, true, (StatusKind.GoodCondition, 2)));
        }

        [Fact]
        public void CanUse_S11NoGoodCondition_UseConditionNotMet()
        {
            var state = Setup(new[] { Phase3Cards.S11 }, new[] { 1 });
            Assert.Equal(UseCheckResult.UseConditionNotMet, Engine.CanUse(state, 1));
        }

        [Fact]
        public void UseCard_S04_GoodScale()
        {
            var state = Setup(new[] { Phase3Cards.S04 }, new[] { 1 }, statuses: Statuses((StatusKind.GoodCondition, 2)));
            Assert.Equal(UseCheckResult.Ok, Engine.CanUse(state, 1));
            Use(state, 1);
            Assert.Equal(28, state.Hp);
            Assert.Equal(20, state.ParamGained);
            Assert.Equal(30, Gained(Phase3Cards.S04, true, (StatusKind.GoodCondition, 2)));
        }

        [Fact]
        public void UseCard_S04WithExcellent_ScalesWithExcellent()
        {
            Assert.Equal(26, Gained(Phase3Cards.S04, false, (StatusKind.GoodCondition, 3), (StatusKind.Excellent, 1)));
            var state = Setup(new[] { Phase3Cards.S04 }, new[] { 1 }, statuses: Statuses((StatusKind.GoodCondition, 1)));
            Assert.Equal(UseCheckResult.UseConditionNotMet, Engine.CanUse(state, 1));
        }

        [Fact]
        public void UseCard_S07_ParamThenGoodCondition()
        {
            var state = Setup(new[] { Phase3Cards.S07 }, new[] { 1 },
                statuses: Statuses((StatusKind.GoodCondition, 3), (StatusKind.Excellent, 2)));
            Use(state, 1);
            Assert.Equal(26, state.Hp);
            Assert.Equal(11, state.ParamGained);
            Assert.Equal(5, state.Statuses.Get(StatusKind.GoodCondition));
        }

        [Fact]
        public void UseCard_S06_ExcellentSkipsFirstDecrement()
        {
            var state = Setup(new[] { Phase3Cards.S06 }, new[] { 1 }, usesLeft: 1,
                statuses: Statuses((StatusKind.GoodCondition, 2)));
            Use(state, 1);
            Assert.Equal(2, state.Turn);
            Assert.Equal(1, state.Statuses.Get(StatusKind.Excellent));
            Assert.Equal(1, state.Statuses.Get(StatusKind.GoodCondition));

            Skip(state);
            Assert.Equal(0, state.Statuses.Get(StatusKind.Excellent));
            Assert.Equal(0, state.Statuses.Get(StatusKind.GoodCondition));
        }

        [Fact]
        public void UseCard_S01_GoodConditionSkipsFirstDecrement()
        {
            var state = Setup(new[] { Phase3Cards.S01 }, new[] { 1 }, usesLeft: 1);
            Use(state, 1);
            Assert.Equal(2, state.Turn);
            Assert.Equal(2, state.Statuses.Get(StatusKind.GoodCondition));

            Skip(state);
            Assert.Equal(1, state.Statuses.Get(StatusKind.GoodCondition));
        }

        [Fact]
        public void Validator_Phase3Cards_NoErrors()
        {
            foreach (var id in Phase3Cards.AllIds)
            {
                Assert.Empty(CardDefinitionValidator.Validate(Catalog.Get(id)));
            }
        }

        [Fact]
        public void Cost_GoodConditionTurns_PaysAndGates()
        {
            var card = TestCards.Card(901, Plan.Free, CardType.Mental, Rarity.N, false, false,
                TestCards.Face(TestCards.StatusCost(CostKind.GoodConditionTurns, 2)), null);
            var engine = new LessonEngine(new TestCardCatalog(new[] { card }));

            var poor = Setup(new[] { 901 }, new[] { 1 }, statuses: Statuses((StatusKind.GoodCondition, 1)));
            Assert.Equal(UseCheckResult.CannotPayCost, engine.CanUse(poor, 1));

            var rich = Setup(new[] { 901 }, new[] { 1 }, statuses: Statuses((StatusKind.GoodCondition, 3)));
            Assert.Equal(UseCheckResult.Ok, engine.CanUse(rich, 1));
            engine.Apply(rich, LessonCommand.UseCard(1), TestStates.NewRandom());
            Assert.Equal(1, rich.Statuses.Get(StatusKind.GoodCondition));
        }

        [Fact]
        public void Cost_ExcellentTurns_PaysAndGates()
        {
            var card = TestCards.Card(901, Plan.Free, CardType.Mental, Rarity.N, false, false,
                TestCards.Face(TestCards.StatusCost(CostKind.ExcellentTurns, 1)), null);
            var engine = new LessonEngine(new TestCardCatalog(new[] { card }));

            var poor = Setup(new[] { 901 }, new[] { 1 });
            Assert.Equal(UseCheckResult.CannotPayCost, engine.CanUse(poor, 1));

            var rich = Setup(new[] { 901 }, new[] { 1 }, statuses: Statuses((StatusKind.Excellent, 1)));
            Assert.Equal(UseCheckResult.Ok, engine.CanUse(rich, 1));
            engine.Apply(rich, LessonCommand.UseCard(1), TestStates.NewRandom());
            Assert.Equal(0, rich.Statuses.Get(StatusKind.Excellent));
        }
    }
}
