using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class Phase4CardsTests
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

        private static int Gained(int cardId, bool enhanced, params (StatusKind Kind, int Value)[] statuses)
        {
            var state = Setup(new[] { cardId }, new[] { 1 }, statuses: Statuses(statuses),
                enhanced: enhanced ? new[] { 1 } : null);
            Use(state, 1);
            return state.ParamGained;
        }

        [Fact]
        public void UseCard_S10_FocusScale()
        {
            var state = Setup(new[] { Phase4Cards.S10 }, new[] { 1 }, statuses: Statuses((StatusKind.Focus, 4)));
            Use(state, 1);
            Assert.Equal(26, state.Hp);
            Assert.Equal(23, state.ParamGained);
            Assert.Equal(31, Gained(Phase4Cards.S10, true, (StatusKind.Focus, 4)));
        }

        [Fact]
        public void UseCard_S05_HitsComputedOneByOne()
        {
            var state = Setup(new[] { Phase4Cards.S05 }, new[] { 1 }, statuses: Statuses((StatusKind.Focus, 3)));
            Assert.Equal(UseCheckResult.Ok, Engine.CanUse(state, 1));
            Use(state, 1);
            Assert.Equal(28, state.Hp);
            Assert.Equal(16, state.ParamGained);
            Assert.Equal(24, Gained(Phase4Cards.S05, true, (StatusKind.Focus, 3)));

            var low = Setup(new[] { Phase4Cards.S05 }, new[] { 1 }, statuses: Statuses((StatusKind.Focus, 2)));
            Assert.Equal(UseCheckResult.UseConditionNotMet, Engine.CanUse(low, 1));
        }

        [Fact]
        public void UseCard_S14_SecondEffectNeedsFocus()
        {
            var state = Setup(new[] { Phase4Cards.S14 }, new[] { 1 }, statuses: Statuses((StatusKind.Focus, 3)));
            Use(state, 1);
            Assert.Equal(24, state.Hp);
            Assert.Equal(36, state.ParamGained);

            Assert.Equal(17, Gained(Phase4Cards.S14, false, (StatusKind.Focus, 2)));
        }

        [Fact]
        public void UseCard_S14Enhanced_SecondEffectScalesFocus()
        {
            Assert.Equal(57, Gained(Phase4Cards.S14, true, (StatusKind.Focus, 3)));
        }

        [Fact]
        public void UseCard_S03_AddsFocusAfterParam()
        {
            var state = Setup(new[] { Phase4Cards.S03 }, new[] { 1 });
            Use(state, 1);
            Assert.Equal(6, state.ParamGained);
            Assert.Equal(2, state.Statuses.Get(StatusKind.Focus));

            var enhanced = Setup(new[] { Phase4Cards.S03 }, new[] { 1 }, enhanced: new[] { 1 });
            Use(enhanced, 1);
            Assert.Equal(8, enhanced.ParamGained);
            Assert.Equal(3, enhanced.Statuses.Get(StatusKind.Focus));
        }

        [Fact]
        public void UseCard_S09_ConditionJudgedBeforeEffects()
        {
            var good = Setup(new[] { Phase4Cards.S09 }, new[] { 1 }, statuses: Statuses((StatusKind.GoodCondition, 2)));
            Use(good, 1);
            Assert.Equal(9, good.ParamGained);
            Assert.Equal(3, good.Statuses.Get(StatusKind.Focus));

            var none = Setup(new[] { Phase4Cards.S09 }, new[] { 1 });
            Use(none, 1);
            Assert.Equal(6, none.ParamGained);
            Assert.Equal(0, none.Statuses.Get(StatusKind.Focus));
        }

        [Fact]
        public void UseCard_S13ThenS08_PersistentAddsFocus()
        {
            var state = Setup(new[] { Phase4Cards.S13, Phase4Cards.S08 }, new[] { 1, 2 });
            Use(state, 1);
            Assert.Equal(26, state.Hp);
            Assert.Equal(2, state.Statuses.Get(StatusKind.Focus));
            Assert.Single(state.Persistents);

            Use(state, 2);
            Assert.Equal(9, state.ParamGained);
            Assert.Equal(5, state.Statuses.Get(StatusKind.Focus));
        }

        [Fact]
        public void UseCard_S12_AddsFocusAndGoodCondition()
        {
            var ids = new[] { Phase4Cards.S12 }.Concat(Enumerable.Repeat(Phase1Cards.F01, 8));
            var state = new LessonState(TestStates.NormalLesson, TestStates.Deck(ids), 30, 30);
            Engine.Start(state, TestStates.NewRandom());
            Assert.Contains(1, state.Zones.Hand);

            Use(state, 1);
            Assert.Equal(26, state.Hp);
            Assert.Equal(3, state.Statuses.Get(StatusKind.Focus));
            Assert.Equal(2, state.Statuses.Get(StatusKind.GoodCondition));
        }

        [Fact]
        public void UseCard_S10EnhancedWithGoodCondition_MultipliesAfterFocus()
        {
            Assert.Equal(47, Gained(Phase4Cards.S10, true, (StatusKind.Focus, 4), (StatusKind.GoodCondition, 2)));
        }

        [Fact]
        public void UseCard_S10EnhancedWithExcellent_AddsExcellentBonus()
        {
            Assert.Equal(53, Gained(Phase4Cards.S10, true,
                (StatusKind.Focus, 4), (StatusKind.GoodCondition, 2), (StatusKind.Excellent, 1)));
        }

        [Fact]
        public void UseCard_S04WithFocusAndGood_FocusBeforeScale()
        {
            var state = Setup(new[] { Phase3Cards.S04 }, new[] { 1 },
                statuses: Statuses((StatusKind.Focus, 4), (StatusKind.GoodCondition, 2)));
            Use(state, 1);
            Assert.Equal(28, state.ParamGained);
        }

        [Fact]
        public void Validator_Phase4Cards_NoErrors()
        {
            foreach (var id in Phase4Cards.AllIds)
            {
                Assert.Empty(CardDefinitionValidator.Validate(Catalog.Get(id)));
            }
        }

        [Fact]
        public void Cost_Focus_PaysAndGates()
        {
            var card = TestCards.Card(901, Plan.Free, CardType.Mental, Rarity.N, false, false,
                TestCards.Face(TestCards.StatusCost(CostKind.Focus, 2)), null);
            var engine = new LessonEngine(new TestCardCatalog(new[] { card }));

            var poor = Setup(new[] { 901 }, new[] { 1 }, statuses: Statuses((StatusKind.Focus, 1)));
            Assert.Equal(UseCheckResult.CannotPayCost, engine.CanUse(poor, 1));

            var rich = Setup(new[] { 901 }, new[] { 1 }, statuses: Statuses((StatusKind.Focus, 3)));
            Assert.Equal(UseCheckResult.Ok, engine.CanUse(rich, 1));
            engine.Apply(rich, LessonCommand.UseCard(1), TestStates.NewRandom());
            Assert.Equal(1, rich.Statuses.Get(StatusKind.Focus));
        }
    }
}
