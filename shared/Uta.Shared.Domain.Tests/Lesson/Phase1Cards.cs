using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// フェーズ1のカード（lesson-card.md 14章・15.2）。ID はテスト用の値。
    /// </summary>
    public sealed class Phase1Cards : ICardCatalog
    {
        public const int F01 = 1;
        public const int F02 = 2;
        public const int F03 = 3;
        public const int F04 = 4;
        public const int F05 = 5;
        public const int F06 = 6;
        public const int L01 = 11;
        public const int L02 = 12;
        public const int L04 = 14;
        public const int L07 = 17;
        public const int L10 = 20;
        public const int L14 = 24;
        public const int L15 = 25;

        /// <summary>開始時手札のテスト用（14章には無い）。消費0 で効果なし。</summary>
        public const int StartInHandDummy = 900;

        private readonly Dictionary<int, CardDefinition> cards = new Dictionary<int, CardDefinition>();

        public Phase1Cards()
        {
            Add(F01, Plan.Free, CardType.Active, Rarity.N, false, false,
                Face(Consume(4), Param(9)),
                Face(Consume(3), Param(14)));
            Add(F02, Plan.Free, CardType.Active, Rarity.N, false, false,
                Face(Consume(3), Param(2), Genki(2)),
                Face(Consume(3), Param(6), Genki(4)));
            Add(F03, Plan.Free, CardType.Mental, Rarity.N, true, false,
                Face(Consume(0), Genki(4)),
                Face(Consume(0), Genki(7)));
            Add(F04, Plan.Free, CardType.Mental, Rarity.R, false, false,
                Face(Consume(0), Genki(2), Turns(StatusKind.CostHalf, 2)),
                Face(Consume(0), Genki(2), Turns(StatusKind.CostHalf, 3)));
            Add(F05, Plan.Free, CardType.Mental, Rarity.R, true, true,
                Face(Consume(0), Genki(3), StackIf(StatusKind.CostReduce, 1, HpAtLeast(50))),
                Face(Consume(0), Genki(6), StackIf(StatusKind.CostReduce, 1, HpAtLeast(50))));
            Add(F06, Plan.Free, CardType.Mental, Rarity.SR, true, true,
                Face(HpDirect(1), Uses(1), Draw(2)),
                Face(Consume(0), Uses(1), Draw(2), Turns(StatusKind.CostHalf, 1)));

            Add(L01, Plan.Logic, CardType.Mental, Rarity.N, false, false,
                Face(Consume(2), Genki(1), Impression(2)),
                Face(Consume(2), Genki(2), Impression(3)));
            Add(L02, Plan.Logic, CardType.Mental, Rarity.N, true, false,
                FaceWithCondition(Consume(2), ImpressionAtLeast(1), Impression(4)),
                FaceWithCondition(Consume(2), ImpressionAtLeast(1), Impression(5)));
            Add(L04, Plan.Logic, CardType.Active, Rarity.N, true, false,
                Face(Consume(5), Impression(2), RatioOfImpression(100)),
                Face(Consume(5), Impression(3), RatioOfImpression(120)));
            Add(L07, Plan.Logic, CardType.Active, Rarity.R, false, false,
                Face(Consume(4), Param(7), Impression(3)),
                Face(Consume(4), Param(9), Impression(4)));
            Add(L10, Plan.Logic, CardType.Active, Rarity.R, true, false,
                Face(Consume(5), RatioOfImpression(150)),
                Face(Consume(5), RatioOfImpression(200)));
            Add(L14, Plan.Logic, CardType.Mental, Rarity.SR, true, true,
                Face(Consume(4), Impression(3), OnMentalUse(Impression(1))),
                Face(Consume(4), Impression(5), OnMentalUse(Impression(1))));
            Add(L15, Plan.Logic, CardType.Active, Rarity.SSR, true, true,
                Face(Consume(6), Impression(5), RatioOfImpression(100)),
                Face(Consume(6), Impression(6), RatioOfImpression(170)));

            cards.Add(StartInHandDummy, new CardDefinition(
                StartInHandDummy, Plan.Free, CardType.Mental, Rarity.N, 1, false, false, true,
                Face(Consume(0)), null));
        }

        public static IReadOnlyList<int> AllPhase1Ids => new[] { F01, F02, F03, F04, F05, F06, L01, L02, L04, L07, L10, L14, L15 };

        public CardDefinition Get(int cardId)
        {
            return cards[cardId];
        }

        private void Add(int id, Plan plan, CardType type, Rarity rarity, bool once, bool unique, CardFace baseFace, CardFace enhanced)
        {
            cards.Add(id, new CardDefinition(id, plan, type, rarity, 1, once, unique, false, baseFace, enhanced));
        }

        private static CardFace Face(CostDef cost, params EffectDef[] effects)
        {
            return new CardFace(new[] { cost }, new ConditionDef[0], effects);
        }

        private static CardFace FaceWithCondition(CostDef cost, ConditionDef useCondition, params EffectDef[] effects)
        {
            return new CardFace(new[] { cost }, new[] { useCondition }, effects);
        }

        private static CostDef Consume(int value)
        {
            return new CostDef(CostKind.Consume, value);
        }

        private static CostDef HpDirect(int value)
        {
            return new CostDef(CostKind.HpDirect, value);
        }

        private static EffectDef Param(int value)
        {
            return new EffectDef(EffectOp.Param, value);
        }

        private static EffectDef Genki(int value)
        {
            return new EffectDef(EffectOp.AddGenki, value);
        }

        private static EffectDef Impression(int value)
        {
            return new EffectDef(EffectOp.AddStack, value, (int)StatusKind.Impression);
        }

        private static EffectDef Turns(StatusKind kind, int value)
        {
            return new EffectDef(EffectOp.AddTurns, value, (int)kind);
        }

        private static EffectDef StackIf(StatusKind kind, int value, ConditionDef condition)
        {
            return new EffectDef(EffectOp.AddStack, value, (int)kind, conditions: new[] { condition });
        }

        private static EffectDef Uses(int value)
        {
            return new EffectDef(EffectOp.AddUses, value);
        }

        private static EffectDef Draw(int value)
        {
            return new EffectDef(EffectOp.Draw, value);
        }

        private static EffectDef RatioOfImpression(int percent)
        {
            return new EffectDef(EffectOp.ParamByRatio, percent, (int)RatioSource.Impression);
        }

        private static EffectDef OnMentalUse(EffectDef effect)
        {
            return new EffectDef(EffectOp.Persistent, 0, (int)PersistentTrigger.OnMentalCardUse, children: new[] { effect });
        }

        private static ConditionDef HpAtLeast(int percent)
        {
            return new ConditionDef(ConditionSubject.HpPercent, Comparator.GreaterOrEqual, percent);
        }

        private static ConditionDef ImpressionAtLeast(int value)
        {
            return new ConditionDef(ConditionSubject.Impression, Comparator.GreaterOrEqual, value);
        }
    }
}
