using System;
using System.Collections.Generic;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>コスト（lesson-card.md 5章）。</summary>
    public readonly struct CostDef
    {
        public CostDef(CostKind kind, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Kind = kind;
            Value = value;
        }

        public CostKind Kind { get; }
        public int Value { get; }
    }

    /// <summary>条件 {対象, 比較, 値}（lesson-card.md 6章）。使用条件と効果内条件の両方に使う。</summary>
    public readonly struct ConditionDef
    {
        public ConditionDef(ConditionSubject subject, Comparator comparator, int value)
        {
            Subject = subject;
            Comparator = comparator;
            Value = value;
        }

        public ConditionSubject Subject { get; }
        public Comparator Comparator { get; }
        public int Value { get; }
    }

    /// <summary>
    /// 効果。マスタと同じ汎用の形で持つ（masterdata-pipeline.md 3章）。引数の意味は lesson-domain-types.md 2.1。
    /// </summary>
    public sealed class EffectDef
    {
        private static readonly ConditionDef[] NoConditions = new ConditionDef[0];
        private static readonly EffectDef[] NoChildren = new EffectDef[0];

        public EffectDef(
            EffectOp op,
            StatusKind status = StatusKind.Nothing,
            PersistentTrigger trigger = PersistentTrigger.Nothing,
            int value = 0,
            int arg1 = 0,
            int arg2 = 0,
            int arg3 = 0,
            int refCardId = 0,
            IReadOnlyList<ConditionDef> conditions = null,
            IReadOnlyList<EffectDef> children = null)
        {
            Op = op;
            Status = status;
            Trigger = trigger;
            Value = value;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            RefCardId = refCardId;
            Conditions = conditions ?? NoConditions;
            Children = children ?? NoChildren;
        }

        public EffectOp Op { get; }

        /// <summary>対象のステータス（AddStatus など）。使わないときは Nothing</summary>
        public StatusKind Status { get; }

        /// <summary>持続効果のトリガー（AddPersistent）。使わないときは Nothing</summary>
        public PersistentTrigger Trigger { get; }

        public int Value { get; }
        public int Arg1 { get; }
        public int Arg2 { get; }
        public int Arg3 { get; }

        /// <summary>他のカードを指すとき（CreateCard）。0 は無し</summary>
        public int RefCardId { get; }

        /// <summary>効果内条件。すべて満たすときだけ発動する</summary>
        public IReadOnlyList<ConditionDef> Conditions { get; }

        /// <summary>ScheduleEffect / AddPersistent が発動する効果</summary>
        public IReadOnlyList<EffectDef> Children { get; }
    }

    /// <summary>カードの片面（通常版 / 強化版）。</summary>
    public sealed class CardFace
    {
        public CardFace(
            IReadOnlyList<CostDef> costs,
            IReadOnlyList<ConditionDef> useConditions,
            IReadOnlyList<EffectDef> effects)
        {
            Costs = costs ?? throw new ArgumentNullException(nameof(costs));
            UseConditions = useConditions ?? throw new ArgumentNullException(nameof(useConditions));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        public IReadOnlyList<CostDef> Costs { get; }
        public IReadOnlyList<ConditionDef> UseConditions { get; }

        /// <summary>記載順に上から処理する</summary>
        public IReadOnlyList<EffectDef> Effects { get; }
    }

    /// <summary>カード定義（lesson-card.md 3章）。マスタの生成物とは別の、Domain の型。</summary>
    public sealed class CardDefinition
    {
        public CardDefinition(
            int id,
            Plan plan,
            CardType type,
            Rarity rarity,
            int unlockLv,
            bool onceInLesson,
            bool unique,
            bool startInHand,
            CardFace baseFace,
            CardFace enhancedFace)
        {
            Id = id;
            Plan = plan;
            Type = type;
            Rarity = rarity;
            UnlockLv = unlockLv;
            OnceInLesson = onceInLesson;
            Unique = unique;
            StartInHand = startInHand;
            Base = baseFace ?? throw new ArgumentNullException(nameof(baseFace));
            Enhanced = enhancedFace;
        }

        public int Id { get; }
        public Plan Plan { get; }
        public CardType Type { get; }
        public Rarity Rarity { get; }
        public int UnlockLv { get; }
        public bool OnceInLesson { get; }
        public bool Unique { get; }
        public bool StartInHand { get; }
        public CardFace Base { get; }

        /// <summary>強化版。不調カードなど強化できないカードは null</summary>
        public CardFace Enhanced { get; }

        /// <summary>強化済み（またはレッスン中強化）なら強化版、なければ通常版。</summary>
        public CardFace GetFace(bool enhanced)
        {
            return enhanced && Enhanced != null ? Enhanced : Base;
        }
    }

    /// <summary>ID からカード定義を返すポート。マスタからの変換は Infrastructure が受け持つ（ADR 0008）。</summary>
    public interface ICardCatalog
    {
        CardDefinition Get(int cardId);
    }

    /// <summary>レッスンのルールの値（lesson-card.md 4.1）。差し替えない（lesson-domain-types.md 2章）。</summary>
    public static class LessonRules
    {
        public const int DrawPerTurn = 3;
        public const int FirstTurnMinHand = 3;
        public const int StartInHandMax = 5;
        public const int BaseUses = 1;
        public const int SkipHpRecover = 2;
    }

    /// <summary>1回のレッスンに渡す値。ターン数・目標は produce.md のマスタから。</summary>
    public sealed class LessonSetup
    {
        /// <param name="perfectTarget">パーフェクト目標。0 は無し（試験など）</param>
        public LessonSetup(LessonKind kind, ParamKind targetParam, int maxTurns, int clearTarget, int perfectTarget)
        {
            if (maxTurns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTurns));
            }

            if (kind == LessonKind.Nothing)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (targetParam == ParamKind.Nothing)
            {
                throw new ArgumentOutOfRangeException(nameof(targetParam));
            }

            Kind = kind;
            TargetParam = targetParam;
            MaxTurns = maxTurns;
            ClearTarget = clearTarget;
            PerfectTarget = perfectTarget;
        }

        public LessonKind Kind { get; }
        public ParamKind TargetParam { get; }
        public int MaxTurns { get; }
        public int ClearTarget { get; }
        public int PerfectTarget { get; }

        public bool IsExam => Kind == LessonKind.MidExam || Kind == LessonKind.FinalExam;
    }
}
