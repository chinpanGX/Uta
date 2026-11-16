using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>テストのカードを組み立てる共通の補助。</summary>
    public static class TestCards
    {
        public static CardDefinition Card(int id, Plan plan, CardType type, Rarity rarity,
            bool once, bool unique, CardFace baseFace, CardFace enhanced, bool startInHand = false)
        {
            return new CardDefinition(id, plan, type, rarity, 1, once, unique, startInHand, baseFace, enhanced);
        }

        public static CardFace Face(CostDef cost, params EffectDef[] effects)
        {
            return new CardFace(new[] { cost }, new ConditionDef[0], effects);
        }

        public static CardFace FaceWithCondition(CostDef cost, ConditionDef useCondition, params EffectDef[] effects)
        {
            return new CardFace(new[] { cost }, new[] { useCondition }, effects);
        }

        public static CostDef Consume(int value)
        {
            return new CostDef(CostKind.Consume, value);
        }

        public static CostDef HpDirect(int value)
        {
            return new CostDef(CostKind.HpDirect, value);
        }

        public static EffectDef Param(int value, int hits = 0, int focusScale = 0, int goodScale = 0)
        {
            return new EffectDef(EffectOp.GainParam, value: value, arg1: hits, arg2: focusScale, arg3: goodScale);
        }

        public static EffectDef Shield(int value)
        {
            return new EffectDef(EffectOp.AddShield, value: value);
        }

        public static EffectDef Impression(int value)
        {
            return new EffectDef(EffectOp.AddStatus, status: StatusKind.Impression, value: value);
        }

        public static EffectDef Turns(StatusKind kind, int value)
        {
            return new EffectDef(EffectOp.AddStatus, status: kind, value: value);
        }

        public static EffectDef Status(StatusKind kind, int value)
        {
            return new EffectDef(EffectOp.AddStatus, status: kind, value: value);
        }

        public static EffectDef StackIf(StatusKind kind, int value, ConditionDef condition)
        {
            return new EffectDef(EffectOp.AddStatus, status: kind, value: value, conditions: new[] { condition });
        }

        public static EffectDef Uses(int value)
        {
            return new EffectDef(EffectOp.AddCardUses, value: value);
        }

        public static EffectDef Draw(int value)
        {
            return new EffectDef(EffectOp.DrawCards, value: value);
        }

        public static EffectDef RatioOfImpression(int percent)
        {
            return new EffectDef(EffectOp.GainParamByImpression, value: percent);
        }

        public static EffectDef OnMentalUse(EffectDef effect)
        {
            return new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.OnMentalCardUse, children: new[] { effect });
        }

        public static ConditionDef HpAtLeast(int percent)
        {
            return new ConditionDef(ConditionSubject.HpPercent, Comparator.GreaterOrEqual, percent);
        }

        public static ConditionDef ImpressionAtLeast(int value)
        {
            return new ConditionDef(ConditionSubject.Impression, Comparator.GreaterOrEqual, value);
        }

        public static CostDef StatusCost(CostKind kind, int value)
        {
            return new CostDef(kind, value);
        }

        public static EffectDef FixedShield(int value)
        {
            return new EffectDef(EffectOp.AddFixedShield, value: value);
        }

        public static EffectDef Recover(int value)
        {
            return new EffectDef(EffectOp.RecoverHp, value: value);
        }

        public static EffectDef ScaleShield(int percent)
        {
            return new EffectDef(EffectOp.ScaleShield, value: percent);
        }

        public static EffectDef SetShield(int value)
        {
            return new EffectDef(EffectOp.SetShield, value: value);
        }

        public static EffectDef ParamByShield(int percent)
        {
            return new EffectDef(EffectOp.GainParamByShield, value: percent);
        }

        public static EffectDef ParamByMotivation(int percent)
        {
            return new EffectDef(EffectOp.GainParamByMotivation, value: percent);
        }

        public static ConditionDef AtLeast(ConditionSubject subject, int value)
        {
            return new ConditionDef(subject, Comparator.GreaterOrEqual, value);
        }

        public static EffectDef OnActiveUse(EffectDef effect)
        {
            return new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.OnActiveCardUse, children: new[] { effect });
        }

        /// <summary>effect と同じ列で、Conditions だけを condition 1 つにした新しい EffectDef を返す。</summary>
        public static EffectDef If(EffectDef effect, ConditionDef condition)
        {
            return new EffectDef(
                effect.Op,
                status: effect.Status,
                trigger: effect.Trigger,
                value: effect.Value,
                arg1: effect.Arg1,
                arg2: effect.Arg2,
                arg3: effect.Arg3,
                refCardId: effect.RefCardId,
                conditions: new[] { condition },
                children: effect.Children);
        }
    }
}
