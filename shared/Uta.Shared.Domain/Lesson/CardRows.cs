namespace Uta.Shared.Domain.Lesson
{
    // マスタの行と同じ列を持つ、Domain の行の型。各側の Infrastructure がマスタの値をそのまま詰めて CardCatalog.Create に渡す（repository-layout.md 4章）。

    /// <summary>cardテーブルの1行。</summary>
    public readonly struct CardRow
    {
        public CardRow(int cardId, Plan plan, CardType type, Rarity rarity, int unlockLv, bool onceInLesson, bool unique, bool startInHand)
        {
            CardId = cardId;
            Plan = plan;
            Type = type;
            Rarity = rarity;
            UnlockLv = unlockLv;
            OnceInLesson = onceInLesson;
            Unique = unique;
            StartInHand = startInHand;
        }

        public int CardId { get; }
        public Plan Plan { get; }
        public CardType Type { get; }
        public Rarity Rarity { get; }
        public int UnlockLv { get; }
        public bool OnceInLesson { get; }
        public bool Unique { get; }
        public bool StartInHand { get; }
    }

    /// <summary>cardFaceテーブルの1行。</summary>
    public readonly struct CardFaceRow
    {
        /// <param name="useConditionGroupId">0 は条件なし</param>
        public CardFaceRow(int cardId, CardFaceKind face, int effectGroupId, int useConditionGroupId)
        {
            CardId = cardId;
            Face = face;
            EffectGroupId = effectGroupId;
            UseConditionGroupId = useConditionGroupId;
        }

        public int CardId { get; }
        public CardFaceKind Face { get; }
        public int EffectGroupId { get; }
        public int UseConditionGroupId { get; }
    }

    /// <summary>cardCostテーブルの1行。</summary>
    public readonly struct CardCostRow
    {
        public CardCostRow(int cardId, CardFaceKind face, int order, CostKind kind, int value)
        {
            CardId = cardId;
            Face = face;
            Order = order;
            Kind = kind;
            Value = value;
        }

        public int CardId { get; }
        public CardFaceKind Face { get; }
        public int Order { get; }
        public CostKind Kind { get; }
        public int Value { get; }
    }

    /// <summary>effectテーブルの1行。</summary>
    public readonly struct EffectRow
    {
        /// <param name="refConditionGroupId">0 は空欄</param>
        /// <param name="refEffectGroupId">0 は空欄</param>
        /// <param name="refCardId">0 は空欄</param>
        public EffectRow(
            int effectGroupId,
            int order,
            EffectOp op,
            StatusKind status,
            PersistentTrigger trigger,
            int value,
            int arg1,
            int arg2,
            int arg3,
            int refConditionGroupId,
            int refEffectGroupId,
            int refCardId)
        {
            EffectGroupId = effectGroupId;
            Order = order;
            Op = op;
            Status = status;
            Trigger = trigger;
            Value = value;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            RefConditionGroupId = refConditionGroupId;
            RefEffectGroupId = refEffectGroupId;
            RefCardId = refCardId;
        }

        public int EffectGroupId { get; }
        public int Order { get; }
        public EffectOp Op { get; }
        public StatusKind Status { get; }
        public PersistentTrigger Trigger { get; }
        public int Value { get; }
        public int Arg1 { get; }
        public int Arg2 { get; }
        public int Arg3 { get; }
        public int RefConditionGroupId { get; }
        public int RefEffectGroupId { get; }
        public int RefCardId { get; }
    }

    /// <summary>conditionテーブルの1行。</summary>
    public readonly struct ConditionRow
    {
        public ConditionRow(int conditionGroupId, int order, ConditionSubject subject, Comparator comparator, int value)
        {
            ConditionGroupId = conditionGroupId;
            Order = order;
            Subject = subject;
            Comparator = comparator;
            Value = value;
        }

        public int ConditionGroupId { get; }
        public int Order { get; }
        public ConditionSubject Subject { get; }
        public Comparator Comparator { get; }
        public int Value { get; }
    }
}
