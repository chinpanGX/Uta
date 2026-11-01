using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class Phase2CardsTests
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

        [Fact]
        public void UseCard_L05_RatioOfShield()
        {
            var state = Setup(new[] { Phase2Cards.L05 }, new[] { 1 }, shield: 8);
            Use(state, 1);
            Assert.Equal(25, state.Hp);
            Assert.Equal(8, state.ParamGained);
            Assert.Equal(8, state.Shield);
        }

        [Fact]
        public void UseCard_L05Enhanced_RatioOfShield()
        {
            var state = Setup(new[] { Phase2Cards.L05 }, new[] { 1 }, shield: 8, enhanced: new[] { 1 });
            Use(state, 1);
            Assert.Equal(27, state.Hp);
            Assert.Equal(9, state.ParamGained);
        }

        [Fact]
        public void UseCard_L13_RatioThenScaleShield()
        {
            var state = Setup(new[] { Phase2Cards.L13 }, new[] { 1 }, shield: 7);
            Use(state, 1);
            Assert.Equal(27, state.Hp);
            Assert.Equal(10, state.ParamGained);
            Assert.Equal(3, state.Shield);
        }

        [Fact]
        public void UseCard_L13Enhanced_RatioThenScaleShield()
        {
            var state = Setup(new[] { Phase2Cards.L13 }, new[] { 1 }, shield: 7, enhanced: new[] { 1 });
            Use(state, 1);
            Assert.Equal(13, state.ParamGained);
            Assert.Equal(3, state.Shield);
        }

        [Fact]
        public void UseCard_L06_RatioOfMotivation()
        {
            var state = Setup(new[] { Phase2Cards.L06 }, new[] { 1 }, statuses: Statuses((StatusKind.Motivation, 5)));
            Use(state, 1);
            Assert.Equal(28, state.Hp);
            Assert.Equal(8, state.Shield);
            Assert.Equal(6, state.ParamGained);
        }

        [Fact]
        public void UseCard_L08_RatioOfShieldAfterMotivation()
        {
            var state = Setup(new[] { Phase2Cards.L08 }, new[] { 1 }, shield: 10);
            Use(state, 1);
            Assert.Equal(26, state.Hp);
            Assert.Equal(3, state.Statuses.Get(StatusKind.Motivation));
            Assert.Equal(6, state.ParamGained);
            Assert.Equal(10, state.Shield);
        }

        [Fact]
        public void UseCard_L03Twice_MotivationAddsToShield()
        {
            var state = Setup(new[] { Phase2Cards.L03, Phase2Cards.L03 }, new[] { 1, 2 });
            Use(state, 1);
            Assert.Equal(28, state.Hp);
            Assert.Equal(1, state.Shield);
            Assert.Equal(2, state.Statuses.Get(StatusKind.Motivation));

            Use(state, 2);
            Assert.Equal(27, state.Hp);
            Assert.Equal(3, state.Shield);
            Assert.Equal(4, state.Statuses.Get(StatusKind.Motivation));
        }

        [Fact]
        public void UseCard_F07ThenL09_ShieldGainBlockedStopsGain()
        {
            var state = Setup(new[] { Phase2Cards.F07, Phase2Cards.L09 }, new[] { 1, 2 }, usesLeft: 3);
            Use(state, 1);
            Assert.Equal(11, state.Shield);
            Assert.Equal(2, state.Statuses.Get(StatusKind.ShieldGainBlock));
            Assert.Equal(3, state.Statuses.Get(StatusKind.CostHalf));

            Use(state, 2);
            Assert.Equal(11, state.Shield);
        }

        [Fact]
        public void UseCard_F07WithDebuffGuard_CancelsShieldGainBlock()
        {
            var state = Setup(new[] { Phase2Cards.F07 }, new[] { 1 }, statuses: Statuses((StatusKind.DebuffGuard, 1)));
            Use(state, 1);
            Assert.Equal(0, state.Statuses.Get(StatusKind.DebuffGuard));
            Assert.Equal(0, state.Statuses.Get(StatusKind.ShieldGainBlock));
            Assert.Equal(11, state.Shield);
            Assert.Equal(3, state.Statuses.Get(StatusKind.CostHalf));
        }

        [Fact]
        public void Validator_Phase2Cards_NoErrors()
        {
            foreach (var id in Phase2Cards.AllIds)
            {
                Assert.Empty(CardDefinitionValidator.Validate(Catalog.Get(id)));
            }
        }

        [Fact]
        public void Start_L12InDeck_StartsInHand()
        {
            var ids = new[] { Phase2Cards.L12 }.Concat(Enumerable.Repeat(Phase1Cards.F01, 8));
            var state = new LessonState(TestStates.NormalLesson, TestStates.Deck(ids), 30, 30);
            Engine.Start(state, TestStates.NewRandom());

            Assert.Equal(3, state.Zones.Hand.Count);
            Assert.Contains(1, state.Zones.Hand);
        }

        [Fact]
        public void UseCard_L12_AddsImpressionAndMotivation()
        {
            var state = Setup(new[] { Phase2Cards.L12 }, new[] { 1 });
            Use(state, 1);
            Assert.Equal(25, state.Hp);
            Assert.Equal(4, state.Statuses.Get(StatusKind.Impression));
            Assert.Equal(3, state.Statuses.Get(StatusKind.Motivation));
        }

        [Fact]
        public void UseCard_L09_AddsMotivationToShield()
        {
            var state = Setup(new[] { Phase2Cards.L09 }, new[] { 1 }, statuses: Statuses((StatusKind.Motivation, 3)));
            Use(state, 1);
            Assert.Equal(9, state.Shield);
        }

        [Fact]
        public void UseCard_L11_Both()
        {
            var normal = Setup(new[] { Phase2Cards.L11 }, new[] { 1 }, shield: 5);
            Use(normal, 1);
            Assert.Equal(26, normal.Hp);
            Assert.Equal(6, normal.ParamGained);

            var enhanced = Setup(new[] { Phase2Cards.L11 }, new[] { 1 }, shield: 5, enhanced: new[] { 1 });
            Use(enhanced, 1);
            Assert.Equal(27, enhanced.Hp);
            Assert.Equal(6, enhanced.ParamGained);
        }
    }
}
