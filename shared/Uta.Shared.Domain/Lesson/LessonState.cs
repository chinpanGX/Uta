using System;
using System.Collections.Generic;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>カードのインスタンス（produce.md 10.4）。インスタンス ID はセッションの中だけの連番。</summary>
    public sealed class CardInstance
    {
        public CardInstance(int instanceId, int cardId, bool enhanced)
        {
            InstanceId = instanceId;
            CardId = cardId;
            Enhanced = enhanced;
        }

        public int InstanceId { get; }
        public int CardId { get; }

        /// <summary>強化（+）済みか。プロデュース中ずっと</summary>
        public bool Enhanced { get; }
    }

    /// <summary>パラメータ上昇量増加の、% と残りターンの組（lesson-card.md 11章）。</summary>
    public sealed class ParamBonusEntry
    {
        public ParamBonusEntry(int percent, int remainingTurns, bool skipNextDecrement)
        {
            Percent = percent;
            RemainingTurns = remainingTurns;
            SkipNextDecrement = skipNextDecrement;
        }

        public int Percent { get; }
        public int RemainingTurns { get; internal set; }
        public bool SkipNextDecrement { get; internal set; }
    }

    /// <summary>発動予約（「次のターン、〜」「Nターン後、〜」）。</summary>
    public sealed class ScheduledEffect
    {
        public ScheduledEffect(int remainingTurns, IReadOnlyList<EffectDef> effects, long grantOrder)
        {
            RemainingTurns = remainingTurns;
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            GrantOrder = grantOrder;
        }

        public int RemainingTurns { get; internal set; }
        public IReadOnlyList<EffectDef> Effects { get; }
        public long GrantOrder { get; }
    }

    /// <summary>持続効果（「以降、〜」「以降のNターンの間、〜」）。</summary>
    public sealed class PersistentEffect
    {
        /// <summary>無制限を表す値。</summary>
        public const int Unlimited = -1;

        public PersistentEffect(
            PersistentTrigger trigger,
            IReadOnlyList<EffectDef> effects,
            int remainingTurns,
            int remainingCount,
            long grantOrder,
            bool skipNextDecrement)
        {
            Trigger = trigger;
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            RemainingTurns = remainingTurns;
            RemainingCount = remainingCount;
            GrantOrder = grantOrder;
            SkipNextDecrement = skipNextDecrement;
        }

        public PersistentTrigger Trigger { get; }
        public IReadOnlyList<EffectDef> Effects { get; }

        /// <summary>残りターン。<see cref="Unlimited"/> は無制限</summary>
        public int RemainingTurns { get; internal set; }

        /// <summary>残り回数。<see cref="Unlimited"/> は無制限</summary>
        public int RemainingCount { get; internal set; }

        public long GrantOrder { get; }
        public bool SkipNextDecrement { get; internal set; }

        public bool IsExpired => RemainingTurns == 0 || RemainingCount == 0;
    }

    /// <summary>
    /// 1回のレッスン・試験の状態。エンジンがその場で書き換える。
    /// サーバーが保存の形に変換できるよう、全部の値を読めて、全部の値から作り直せる（produce.md 10.5）。
    /// </summary>
    public sealed class LessonState
    {
        private readonly Dictionary<int, CardInstance> cards;
        private readonly List<CardInstance> cardList;
        private readonly List<ParamBonusEntry> paramBonuses;
        private readonly List<ScheduledEffect> scheduled;
        private readonly List<PersistentEffect> persistents;
        private readonly SortedSet<int> lessonEnhanced;

        /// <summary>これから始めるレッスンの状態。</summary>
        public LessonState(LessonSetup setup, IReadOnlyList<CardInstance> deck, int hp, int maxHp)
            : this(
                setup,
                deck,
                new Zones(),
                new StatusSet(),
                new ParamBonusEntry[0],
                new ScheduledEffect[0],
                new PersistentEffect[0],
                new int[0],
                0,
                LessonPhase.NotStarted,
                hp,
                maxHp,
                0,
                0,
                0,
                0)
        {
        }

        /// <summary>保存した状態から作り直す。</summary>
        public LessonState(
            LessonSetup setup,
            IReadOnlyList<CardInstance> deck,
            Zones zones,
            StatusSet statuses,
            IEnumerable<ParamBonusEntry> paramBonuses,
            IEnumerable<ScheduledEffect> scheduled,
            IEnumerable<PersistentEffect> persistents,
            IEnumerable<int> lessonEnhanced,
            int turn,
            LessonPhase phase,
            int hp,
            int maxHp,
            int shield,
            int usesLeft,
            int paramGained,
            long nextGrantOrder)
        {
            if (maxHp <= 0 || hp < 0 || hp > maxHp)
            {
                throw new ArgumentOutOfRangeException(nameof(hp));
            }

            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            cardList = new List<CardInstance>(deck ?? throw new ArgumentNullException(nameof(deck)));
            cards = new Dictionary<int, CardInstance>();
            foreach (var card in cardList)
            {
                cards.Add(card.InstanceId, card);
            }

            Zones = zones ?? throw new ArgumentNullException(nameof(zones));
            Statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
            this.paramBonuses = new List<ParamBonusEntry>(paramBonuses);
            this.scheduled = new List<ScheduledEffect>(scheduled);
            this.persistents = new List<PersistentEffect>(persistents);
            this.lessonEnhanced = new SortedSet<int>(lessonEnhanced);
            Turn = turn;
            Phase = phase;
            Hp = hp;
            MaxHp = maxHp;
            Shield = shield;
            UsesLeft = usesLeft;
            ParamGained = paramGained;
            NextGrantOrder = nextGrantOrder;
        }

        public LessonSetup Setup { get; }

        /// <summary>このレッスンに持ち込んだカード（インスタンス ID の昇順とは限らない）</summary>
        public IReadOnlyList<CardInstance> Deck => cardList;

        public Zones Zones { get; }
        public StatusSet Statuses { get; }
        public IReadOnlyList<ParamBonusEntry> ParamBonuses => paramBonuses;
        public IReadOnlyList<ScheduledEffect> Scheduled => scheduled;
        public IReadOnlyList<PersistentEffect> Persistents => persistents;

        /// <summary>レッスン中強化したカードのインスタンス ID（昇順）</summary>
        public IReadOnlyCollection<int> LessonEnhanced => lessonEnhanced;

        /// <summary>何ターン目か（1 から）。開始前は 0</summary>
        public int Turn { get; internal set; }

        public LessonPhase Phase { get; internal set; }

        /// <summary>体力。プロデュース全体で持ち越すので、終了後に呼び出し側が読み戻す</summary>
        public int Hp { get; internal set; }

        public int MaxHp { get; }
        public int Shield { get; internal set; }

        /// <summary>このターンに残っているスキルカードの使用数</summary>
        public int UsesLeft { get; internal set; }

        /// <summary>このレッスンで得たパラメータ。パーフェクト目標で止まる</summary>
        public int ParamGained { get; internal set; }

        /// <summary>次に付与する持続効果・発動予約の順番</summary>
        public long NextGrantOrder { get; internal set; }

        public bool IsFinished => Phase == LessonPhase.Finished;

        public CardInstance GetCard(int instanceId)
        {
            if (!cards.TryGetValue(instanceId, out var card))
            {
                throw new ArgumentException("カードがありません: " + instanceId, nameof(instanceId));
            }

            return card;
        }

        /// <summary>強化版として扱うか（強化済み、またはレッスン中強化）。</summary>
        public bool IsTreatedAsEnhanced(int instanceId)
        {
            return GetCard(instanceId).Enhanced || lessonEnhanced.Contains(instanceId);
        }

        internal void AddParamBonus(ParamBonusEntry entry)
        {
            paramBonuses.Add(entry);
        }

        internal void AddScheduled(ScheduledEffect effect)
        {
            scheduled.Add(effect);
        }

        internal void AddPersistent(PersistentEffect effect)
        {
            persistents.Add(effect);
        }

        internal void RemoveScheduled(ScheduledEffect effect)
        {
            scheduled.Remove(effect);
        }

        internal void RemoveExpired()
        {
            paramBonuses.RemoveAll(x => x.RemainingTurns <= 0);
            persistents.RemoveAll(x => x.IsExpired);
        }

        internal long TakeGrantOrder()
        {
            return NextGrantOrder++;
        }
    }
}
