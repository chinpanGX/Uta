using System;
using System.Collections.Generic;
using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    public enum EffectOp
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GainParam")] GainParam = 1,
        [InputName("GainParamByImpression")] GainParamByImpression = 2,
        [InputName("GainParamByShield")] GainParamByShield = 3,
        [InputName("GainParamByMotivation")] GainParamByMotivation = 4,
        [InputName("AddParamBonus")] AddParamBonus = 5,
        [InputName("AddShield")] AddShield = 6,
        [InputName("AddFixedShield")] AddFixedShield = 7,
        [InputName("RecoverHp")] RecoverHp = 8,
        [InputName("ScaleShield")] ScaleShield = 9,
        [InputName("SetShield")] SetShield = 10,
        [InputName("AddStatus")] AddStatus = 11,
        [InputName("AddStatusPercent")] AddStatusPercent = 12,
        [InputName("DrawCards")] DrawCards = 13,
        [InputName("AddCardUses")] AddCardUses = 14,
        [InputName("ReplaceHand")] ReplaceHand = 15,
        [InputName("EnhanceHand")] EnhanceHand = 16,
        [InputName("CreateCard")] CreateCard = 17,
        [InputName("ScheduleEffect")] ScheduleEffect = 18,
        [InputName("AddPersistent")] AddPersistent = 19,
    }

    public enum PersistentTrigger
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("LessonStart")] LessonStart = 1,
        [InputName("TurnStart")] TurnStart = 2,
        [InputName("AfterTurnStart")] AfterTurnStart = 3,
        [InputName("EveryNTurns")] EveryNTurns = 4,
        [InputName("OnCostHpDecrease")] OnCostHpDecrease = 5,
        [InputName("OnCardUse")] OnCardUse = 6,
        [InputName("OnActiveCardUse")] OnActiveCardUse = 7,
        [InputName("OnMentalCardUse")] OnMentalCardUse = 8,
        [InputName("AfterCardUse")] AfterCardUse = 9,
        [InputName("AfterActiveCardUse")] AfterActiveCardUse = 10,
        [InputName("AfterMentalCardUse")] AfterMentalCardUse = 11,
        [InputName("OnSkip")] OnSkip = 12,
        [InputName("TurnEnd")] TurnEnd = 13,
    }

    public enum StatusKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GoodCondition")] GoodCondition = 1,
        [InputName("Excellent")] Excellent = 2,
        [InputName("Focus")] Focus = 3,
        [InputName("FocusBoost")] FocusBoost = 4,
        [InputName("Impression")] Impression = 5,
        [InputName("ImpressionBoost")] ImpressionBoost = 6,
        [InputName("Motivation")] Motivation = 7,
        [InputName("CostHalf")] CostHalf = 8,
        [InputName("CostDouble")] CostDouble = 9,
        [InputName("CostReduce")] CostReduce = 10,
        [InputName("ShieldGainBlock")] ShieldGainBlock = 11,
        [InputName("DebuffGuard")] DebuffGuard = 12,
    }

    public enum CostKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Consume")] Consume = 1,
        [InputName("HpDirect")] HpDirect = 2,
        [InputName("GoodConditionTurns")] GoodConditionTurns = 3,
        [InputName("ExcellentTurns")] ExcellentTurns = 4,
        [InputName("Focus")] Focus = 5,
        [InputName("Impression")] Impression = 6,
        [InputName("Motivation")] Motivation = 7,
    }

    public enum ConditionSubject
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GoodConditionTurns")] GoodConditionTurns = 1,
        [InputName("ExcellentTurns")] ExcellentTurns = 2,
        [InputName("Focus")] Focus = 3,
        [InputName("Impression")] Impression = 4,
        [InputName("Motivation")] Motivation = 5,
        [InputName("Shield")] Shield = 6,
        [InputName("HpPercent")] HpPercent = 7,
        [InputName("TurnNumber")] TurnNumber = 8,
        [InputName("ClearTargetPercent")] ClearTargetPercent = 9,
    }

    public enum Comparator
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("GreaterOrEqual")] GreaterOrEqual = 1,
        [InputName("LessOrEqual")] LessOrEqual = 2,
        [InputName("Equal")] Equal = 3,
    }

    public enum CardFaceKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Normal")] Normal = 1,
        [InputName("Enhanced")] Enhanced = 2,
    }

    [MemoryTable("card"), MessagePackObject(true)]
    public class Card : IValidatable<Card>
    {
        [PrimaryKey]
        public int CardId { get; init; }

        public Plan Plan { get; init; }

        public CardType Type { get; init; }

        public Rarity Rarity { get; init; }

        public int UnlockLv { get; init; }

        public bool OnceInLesson { get; init; }

        public bool Unique { get; init; }

        public bool StartInHand { get; init; }

        void IValidatable<Card>.Validate(IValidator<Card> validator)
        {
            validator.Validate(x => x.Plan != Plan.Nothing, "card: Plan が Nothing");
            validator.Validate(x => x.Type != CardType.Nothing, "card: Type が Nothing");
            validator.Validate(x => x.Rarity != Rarity.Nothing, "card: Rarity が Nothing");
            validator.Validate(x => x.UnlockLv >= 1, "card: UnlockLv が 1 未満");
        }
    }

    [MemoryTable("cardFace"), MessagePackObject(true)]
    public class CardFace : IValidatable<CardFace>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Card), nameof(Card.CardId))]
        public int CardId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public CardFaceKind Face { get; init; }

        [Exists(typeof(Effect), nameof(Effect.EffectGroupId))]
        public int EffectGroupId { get; init; }

        [Exists(typeof(Condition), nameof(Condition.ConditionGroupId), AllowEmpty = true)]
        public int UseConditionGroupId { get; init; }

        void IValidatable<CardFace>.Validate(IValidator<CardFace> validator)
        {
            validator.Validate(x => x.Face != CardFaceKind.Nothing, "cardFace: Face が Nothing");
            validator.Validate(x => x.EffectGroupId != 0, "cardFace: EffectGroupId が 0");
        }
    }

    [MemoryTable("cardCost"), MessagePackObject(true)]
    public class CardCost : IValidatable<CardCost>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Card), nameof(Card.CardId))]
        public int CardId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public CardFaceKind Face { get; init; }

        [PrimaryKey(keyOrder: 2)]
        public int Order { get; init; }

        public CostKind Kind { get; init; }

        public int Value { get; init; }

        void IValidatable<CardCost>.Validate(IValidator<CardCost> validator)
        {
            validator.Validate(x => x.Face != CardFaceKind.Nothing, "cardCost: Face が Nothing");
            validator.Validate(x => x.Kind != CostKind.Nothing, "cardCost: Kind が Nothing");
            validator.Validate(x => x.Value >= 0, "cardCost: Value が負");
            validator.Validate(x => x.Order >= 1, "cardCost: Order が 1 未満");
        }
    }

    [MemoryTable("effect"), MessagePackObject(true)]
    public class Effect : IValidatable<Effect>
    {
        [PrimaryKey(keyOrder: 0)]
        public int EffectGroupId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Order { get; init; }

        public EffectOp Op { get; init; }

        public StatusKind Status { get; init; }

        public PersistentTrigger Trigger { get; init; }

        public int Value { get; init; }

        public int Arg1 { get; init; }

        public int Arg2 { get; init; }

        public int Arg3 { get; init; }

        [Exists(typeof(Condition), nameof(Condition.ConditionGroupId), AllowEmpty = true)]
        public int RefConditionGroupId { get; init; }

        [Exists(typeof(Effect), nameof(Effect.EffectGroupId), AllowEmpty = true)]
        public int RefEffectGroupId { get; init; }

        [Exists(typeof(Card), nameof(Card.CardId), AllowEmpty = true)]
        public int RefCardId { get; init; }

        void IValidatable<Effect>.Validate(IValidator<Effect> validator)
        {
            validator.Validate(x => x.Order >= 1, "effect: Order が 1 未満");
            validator.Validate(x => x.Op != EffectOp.Nothing, "effect: Op が Nothing");
            validator.Validate(x => x.Value >= 0 && x.Arg1 >= 0 && x.Arg2 >= 0 && x.Arg3 >= 0, "effect: Value か Arg が負");

            // Op ごとの規則（使う列の下限と、使わない列が空欄であること）
            validator.Validate(x => x.Op != EffectOp.GainParam || x.Value >= 1, "effect: GainParam の Value が 1 未満");
            validator.Validate(
                x => x.Op != EffectOp.GainParam || OnlyUses(x, value: true, arg1: true, arg2: true, arg3: true),
                "effect: GainParam に使わない列がある");

            validator.Validate(x => x.Op != EffectOp.GainParamByImpression || x.Value >= 1, "effect: GainParamByImpression の Value が 1 未満");
            validator.Validate(
                x => x.Op != EffectOp.GainParamByImpression || OnlyUses(x, value: true),
                "effect: GainParamByImpression に使わない列がある");
            validator.Validate(x => x.Op != EffectOp.GainParamByShield || x.Value >= 1, "effect: GainParamByShield の Value が 1 未満");
            validator.Validate(
                x => x.Op != EffectOp.GainParamByShield || OnlyUses(x, value: true),
                "effect: GainParamByShield に使わない列がある");
            validator.Validate(x => x.Op != EffectOp.GainParamByMotivation || x.Value >= 1, "effect: GainParamByMotivation の Value が 1 未満");
            validator.Validate(
                x => x.Op != EffectOp.GainParamByMotivation || OnlyUses(x, value: true),
                "effect: GainParamByMotivation に使わない列がある");

            validator.Validate(x => x.Op != EffectOp.AddParamBonus || (x.Value >= 1 && x.Arg1 >= 1), "effect: AddParamBonus の Value か Arg1 が 1 未満");
            validator.Validate(
                x => x.Op != EffectOp.AddParamBonus || OnlyUses(x, value: true, arg1: true),
                "effect: AddParamBonus に使わない列がある");

            validator.Validate(x => x.Op != EffectOp.AddShield || x.Value >= 1, "effect: AddShield の Value が 1 未満");
            validator.Validate(x => x.Op != EffectOp.AddShield || OnlyUses(x, value: true), "effect: AddShield に使わない列がある");
            validator.Validate(x => x.Op != EffectOp.AddFixedShield || x.Value >= 1, "effect: AddFixedShield の Value が 1 未満");
            validator.Validate(x => x.Op != EffectOp.AddFixedShield || OnlyUses(x, value: true), "effect: AddFixedShield に使わない列がある");
            validator.Validate(x => x.Op != EffectOp.RecoverHp || x.Value >= 1, "effect: RecoverHp の Value が 1 未満");
            validator.Validate(x => x.Op != EffectOp.RecoverHp || OnlyUses(x, value: true), "effect: RecoverHp に使わない列がある");
            validator.Validate(x => x.Op != EffectOp.DrawCards || x.Value >= 1, "effect: DrawCards の Value が 1 未満");
            validator.Validate(x => x.Op != EffectOp.DrawCards || OnlyUses(x, value: true), "effect: DrawCards に使わない列がある");
            validator.Validate(x => x.Op != EffectOp.AddCardUses || x.Value >= 1, "effect: AddCardUses の Value が 1 未満");
            validator.Validate(x => x.Op != EffectOp.AddCardUses || OnlyUses(x, value: true), "effect: AddCardUses に使わない列がある");

            validator.Validate(x => x.Op != EffectOp.ScaleShield || (x.Value >= 0 && x.Value <= 100), "effect: ScaleShield の Value が 0〜100 の外");
            validator.Validate(x => x.Op != EffectOp.ScaleShield || OnlyUses(x, value: true), "effect: ScaleShield に使わない列がある");
            validator.Validate(x => x.Op != EffectOp.SetShield || x.Value >= 0, "effect: SetShield の Value が負");
            validator.Validate(x => x.Op != EffectOp.SetShield || OnlyUses(x, value: true), "effect: SetShield に使わない列がある");

            validator.Validate(
                x => x.Op != EffectOp.AddStatus
                    || (x.Status != StatusKind.Nothing && x.Status != StatusKind.FocusBoost && x.Status != StatusKind.ImpressionBoost),
                "effect: AddStatus の Status が Nothing か FocusBoost か ImpressionBoost");
            validator.Validate(x => x.Op != EffectOp.AddStatus || x.Value >= 1, "effect: AddStatus の Value が 1 未満");
            validator.Validate(
                x => x.Op != EffectOp.AddStatus || OnlyUses(x, value: true, status: true),
                "effect: AddStatus に使わない列がある");

            validator.Validate(
                x => x.Op != EffectOp.AddStatusPercent || x.Status == StatusKind.FocusBoost || x.Status == StatusKind.ImpressionBoost,
                "effect: AddStatusPercent の Status が FocusBoost でも ImpressionBoost でもない");
            validator.Validate(x => x.Op != EffectOp.AddStatusPercent || x.Value >= 1, "effect: AddStatusPercent の Value が 1 未満");
            validator.Validate(
                x => x.Op != EffectOp.AddStatusPercent || OnlyUses(x, value: true, arg1: true, status: true),
                "effect: AddStatusPercent に使わない列がある");

            validator.Validate(x => x.Op != EffectOp.ReplaceHand || OnlyUses(x), "effect: ReplaceHand に使わない列がある");
            validator.Validate(x => x.Op != EffectOp.EnhanceHand || OnlyUses(x), "effect: EnhanceHand に使わない列がある");

            validator.Validate(x => x.Op != EffectOp.CreateCard || x.Value >= 1, "effect: CreateCard の Value が 1 未満");
            validator.Validate(x => x.Op != EffectOp.CreateCard || x.RefCardId != 0, "effect: CreateCard の RefCardId が空欄");
            validator.Validate(
                x => x.Op != EffectOp.CreateCard || OnlyUses(x, value: true, refCard: true),
                "effect: CreateCard に使わない列がある");

            validator.Validate(x => x.Op != EffectOp.ScheduleEffect || x.Value >= 1, "effect: ScheduleEffect の Value が 1 未満");
            validator.Validate(x => x.Op != EffectOp.ScheduleEffect || x.RefEffectGroupId != 0, "effect: ScheduleEffect の RefEffectGroupId が空欄");
            validator.Validate(
                x => x.Op != EffectOp.ScheduleEffect || OnlyUses(x, value: true, refEffect: true),
                "effect: ScheduleEffect に使わない列がある");

            validator.Validate(
                x => x.Op != EffectOp.AddPersistent || (x.Trigger != PersistentTrigger.Nothing && x.Trigger != PersistentTrigger.EveryNTurns),
                "effect: AddPersistent の Trigger が Nothing か EveryNTurns");
            validator.Validate(x => x.Op != EffectOp.AddPersistent || x.RefEffectGroupId != 0, "effect: AddPersistent の RefEffectGroupId が空欄");
            validator.Validate(
                x => x.Op != EffectOp.AddPersistent || OnlyUses(x, arg1: true, arg2: true, trigger: true, refEffect: true),
                "effect: AddPersistent に使わない列がある");

            // 入れ子の深さは 1（表全体を 1 回だけ見る）
            if (validator.CallOnce())
            {
                var rows = validator.GetTableSet().TableData;
                var childGroups = new HashSet<int>();
                foreach (var row in rows)
                {
                    if (row.RefEffectGroupId != 0)
                    {
                        childGroups.Add(row.RefEffectGroupId);
                    }
                }

                foreach (var row in rows)
                {
                    if (childGroups.Contains(row.EffectGroupId) && (row.Op == EffectOp.AddPersistent || row.Op == EffectOp.ScheduleEffect))
                    {
                        validator.Fail("effect: 子のグループ " + row.EffectGroupId + " に AddPersistent / ScheduleEffect がある");
                    }
                }
            }
        }

        // 使う列を true で渡す。渡さなかった列がすべて空欄（0 / Nothing）なら true
        private static bool OnlyUses(
            Effect x, bool value = false, bool arg1 = false, bool arg2 = false, bool arg3 = false,
            bool status = false, bool trigger = false, bool refEffect = false, bool refCard = false)
        {
            return (value || x.Value == 0) && (arg1 || x.Arg1 == 0) && (arg2 || x.Arg2 == 0) && (arg3 || x.Arg3 == 0)
                && (status || x.Status == StatusKind.Nothing) && (trigger || x.Trigger == PersistentTrigger.Nothing)
                && (refEffect || x.RefEffectGroupId == 0) && (refCard || x.RefCardId == 0);
        }
    }

    [MemoryTable("condition"), MessagePackObject(true)]
    public class Condition : IValidatable<Condition>
    {
        [PrimaryKey(keyOrder: 0)]
        public int ConditionGroupId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Order { get; init; }

        public ConditionSubject Subject { get; init; }

        public Comparator Comparator { get; init; }

        public int Value { get; init; }

        void IValidatable<Condition>.Validate(IValidator<Condition> validator)
        {
            validator.Validate(x => x.Subject != ConditionSubject.Nothing, "condition: Subject が Nothing");
            validator.Validate(x => x.Comparator != Comparator.Nothing, "condition: Comparator が Nothing");
            validator.Validate(x => x.Order >= 1, "condition: Order が 1 未満");
            validator.Validate(x => x.Value >= 0, "condition: Value が負");
        }
    }
}
