using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class CardCatalogTests
    {
        private sealed class Rows
        {
            public List<CardRow> Cards { get; } = new List<CardRow>();
            public List<CardFaceRow> Faces { get; } = new List<CardFaceRow>();
            public List<CardCostRow> Costs { get; } = new List<CardCostRow>();
            public List<EffectRow> Effects { get; } = new List<EffectRow>();
            public List<ConditionRow> Conditions { get; } = new List<ConditionRow>();

            public CardCatalog Create()
            {
                return CardCatalog.Create(Cards, Faces, Costs, Effects, Conditions);
            }
        }

        private static CardRow Card(int id, bool once = false)
        {
            return new CardRow(id, Plan.Free, CardType.Active, Rarity.N, 1, once, false, false);
        }

        private static EffectRow Gain(int group, int order, int value)
        {
            return new EffectRow(group, order, EffectOp.GainParam, StatusKind.Nothing, PersistentTrigger.Nothing, value, 0, 0, 0, 0, 0, 0);
        }

        private static EffectRow Status(int group, int order, StatusKind status, int value, int refConditionGroupId = 0)
        {
            return new EffectRow(group, order, EffectOp.AddStatus, status, PersistentTrigger.Nothing, value, 0, 0, 0, refConditionGroupId, 0, 0);
        }

        private static Rows OneCard()
        {
            var rows = new Rows();
            rows.Cards.Add(Card(1));
            rows.Faces.Add(new CardFaceRow(1, CardFaceKind.Normal, 10, 0));
            rows.Costs.Add(new CardCostRow(1, CardFaceKind.Normal, 1, CostKind.Consume, 4));
            rows.Effects.Add(Gain(10, 1, 9));
            return rows;
        }

        [Fact]
        public void Create_OneCardWithBothFaces_BuildsDefinition()
        {
            var rows = OneCard();
            rows.Faces.Add(new CardFaceRow(1, CardFaceKind.Enhanced, 11, 0));
            rows.Costs.Add(new CardCostRow(1, CardFaceKind.Enhanced, 1, CostKind.Consume, 3));
            rows.Effects.Add(Gain(11, 1, 14));

            var card = rows.Create().Get(1);

            Assert.Equal(Plan.Free, card.Plan);
            Assert.Equal(CardType.Active, card.Type);
            Assert.Equal(Rarity.N, card.Rarity);
            Assert.False(card.OnceInLesson);
            Assert.Single(card.Base.Costs);
            Assert.Equal(CostKind.Consume, card.Base.Costs[0].Kind);
            Assert.Equal(4, card.Base.Costs[0].Value);
            Assert.Single(card.Base.Effects);
            Assert.Equal(EffectOp.GainParam, card.Base.Effects[0].Op);
            Assert.Equal(9, card.Base.Effects[0].Value);
            Assert.NotNull(card.Enhanced);
            Assert.Equal(3, card.Enhanced.Costs[0].Value);
            Assert.Equal(14, card.Enhanced.Effects[0].Value);
        }

        [Fact]
        public void Create_NoEnhancedFace_EnhancedIsNull()
        {
            Assert.Null(OneCard().Create().Get(1).Enhanced);
        }

        [Fact]
        public void Create_CostsAndEffects_OrderedByOrder()
        {
            var rows = OneCard();
            rows.Costs.Clear();
            rows.Effects.Clear();
            rows.Costs.Add(new CardCostRow(1, CardFaceKind.Normal, 2, CostKind.HpDirect, 2));
            rows.Costs.Add(new CardCostRow(1, CardFaceKind.Normal, 1, CostKind.Consume, 4));
            rows.Effects.Add(Gain(10, 2, 20));
            rows.Effects.Add(Gain(10, 1, 10));

            var face = rows.Create().Get(1).Base;

            Assert.Equal(CostKind.Consume, face.Costs[0].Kind);
            Assert.Equal(CostKind.HpDirect, face.Costs[1].Kind);
            Assert.Equal(10, face.Effects[0].Value);
            Assert.Equal(20, face.Effects[1].Value);
        }

        [Fact]
        public void Create_UseConditionGroup_BuildsConditions()
        {
            var rows = OneCard();
            rows.Faces.Clear();
            rows.Faces.Add(new CardFaceRow(1, CardFaceKind.Normal, 10, 20));
            rows.Conditions.Add(new ConditionRow(20, 2, ConditionSubject.Focus, Comparator.LessOrEqual, 5));
            rows.Conditions.Add(new ConditionRow(20, 1, ConditionSubject.GoodConditionTurns, Comparator.GreaterOrEqual, 2));

            var conditions = rows.Create().Get(1).Base.UseConditions;

            Assert.Equal(2, conditions.Count);
            Assert.Equal(ConditionSubject.GoodConditionTurns, conditions[0].Subject);
            Assert.Equal(Comparator.GreaterOrEqual, conditions[0].Comparator);
            Assert.Equal(2, conditions[0].Value);
            Assert.Equal(ConditionSubject.Focus, conditions[1].Subject);
        }

        [Fact]
        public void Create_EffectWithCondition_BuildsEffectConditions()
        {
            var rows = OneCard();
            rows.Effects.Add(Status(10, 2, StatusKind.Focus, 1, 30));
            rows.Conditions.Add(new ConditionRow(30, 1, ConditionSubject.Focus, Comparator.GreaterOrEqual, 3));

            var effects = rows.Create().Get(1).Base.Effects;

            Assert.Empty(effects[0].Conditions);
            Assert.Single(effects[1].Conditions);
            Assert.Equal(ConditionSubject.Focus, effects[1].Conditions[0].Subject);
            Assert.Equal(3, effects[1].Conditions[0].Value);
        }

        [Fact]
        public void Create_PersistentWithChildGroup_BuildsChildren()
        {
            var rows = OneCard();
            rows.Effects.Clear();
            rows.Effects.Add(new EffectRow(10, 1, EffectOp.AddPersistent, StatusKind.Nothing, PersistentTrigger.OnActiveCardUse, 0, 0, 0, 0, 0, 11, 0));
            rows.Effects.Add(Status(11, 1, StatusKind.Focus, 1));

            var effect = rows.Create().Get(1).Base.Effects[0];

            Assert.Equal(PersistentTrigger.OnActiveCardUse, effect.Trigger);
            Assert.Single(effect.Children);
            Assert.Equal(EffectOp.AddStatus, effect.Children[0].Op);
            Assert.Equal(StatusKind.Focus, effect.Children[0].Status);
            Assert.Equal(1, effect.Children[0].Value);
        }

        [Fact]
        public void Create_MissingNormalFace_Throws()
        {
            var rows = OneCard();
            rows.Faces.Clear();
            rows.Faces.Add(new CardFaceRow(1, CardFaceKind.Enhanced, 10, 0));

            var exception = Assert.Throws<InvalidOperationException>(() => rows.Create());
            Assert.Contains("カード 1", exception.Message);
        }

        [Fact]
        public void Create_DuplicateCardId_Throws()
        {
            var rows = OneCard();
            rows.Cards.Add(Card(1));

            var exception = Assert.Throws<InvalidOperationException>(() => rows.Create());
            Assert.Contains("カード 1 が重複", exception.Message);
        }

        [Fact]
        public void Create_MissingEffectGroup_Throws()
        {
            var rows = OneCard();
            rows.Effects.Clear();

            var exception = Assert.Throws<InvalidOperationException>(() => rows.Create());
            Assert.Contains("効果グループ 10 がない", exception.Message);
        }

        [Fact]
        public void Create_MissingConditionGroup_Throws()
        {
            var rows = OneCard();
            rows.Effects.Add(Status(10, 2, StatusKind.Focus, 1, 99));

            var exception = Assert.Throws<InvalidOperationException>(() => rows.Create());
            Assert.Contains("条件グループ 99 がない", exception.Message);
        }

        [Fact]
        public void Create_CyclicEffectGroup_Throws()
        {
            var rows = OneCard();
            rows.Effects.Clear();
            rows.Effects.Add(new EffectRow(10, 1, EffectOp.AddPersistent, StatusKind.Nothing, PersistentTrigger.TurnStart, 0, 0, 0, 0, 0, 10, 0));

            Assert.Throws<InvalidOperationException>(() => rows.Create());
        }

        [Fact]
        public void Create_UnsupportedOp_ThrowsWithValidatorMessage()
        {
            var rows = OneCard();
            rows.Effects.Clear();
            rows.Effects.Add(new EffectRow(10, 1, EffectOp.AddParamBonus, StatusKind.Nothing, PersistentTrigger.Nothing, 1, 0, 0, 0, 0, 0, 0));

            var exception = Assert.Throws<InvalidOperationException>(() => rows.Create());
            Assert.Contains("未対応の効果", exception.Message);
        }

        [Fact]
        public void Create_NullArgument_Throws()
        {
            var rows = OneCard();
            Assert.Throws<ArgumentNullException>(() => CardCatalog.Create(null, rows.Faces, rows.Costs, rows.Effects, rows.Conditions));
            Assert.Throws<ArgumentNullException>(() => CardCatalog.Create(rows.Cards, null, rows.Costs, rows.Effects, rows.Conditions));
            Assert.Throws<ArgumentNullException>(() => CardCatalog.Create(rows.Cards, rows.Faces, null, rows.Effects, rows.Conditions));
            Assert.Throws<ArgumentNullException>(() => CardCatalog.Create(rows.Cards, rows.Faces, rows.Costs, null, rows.Conditions));
            Assert.Throws<ArgumentNullException>(() => CardCatalog.Create(rows.Cards, rows.Faces, rows.Costs, rows.Effects, null));
        }

        [Fact]
        public void Get_Unknown_ThrowsKeyNotFound()
        {
            Assert.Throws<KeyNotFoundException>(() => OneCard().Create().Get(2));
        }

        [Fact]
        public void CardIds_Ascending()
        {
            var rows = OneCard();
            foreach (var id in new[] { 5, 3 })
            {
                rows.Cards.Add(Card(id));
                rows.Faces.Add(new CardFaceRow(id, CardFaceKind.Normal, 10, 0));
            }

            Assert.Equal(new[] { 1, 3, 5 }, rows.Create().CardIds);
        }
    }
}
