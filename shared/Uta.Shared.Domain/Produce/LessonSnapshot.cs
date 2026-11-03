using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>カードのインスタンスの保存用の写し。</summary>
    public sealed class CardInstanceSnapshot
    {
        public int InstanceId { get; set; }
        public int CardId { get; set; }
        public bool Enhanced { get; set; }
    }

    /// <summary>パラメータ上昇量増加の保存用の写し。</summary>
    public sealed class ParamBonusSnapshot
    {
        public int Percent { get; set; }
        public int RemainingTurns { get; set; }
        public bool SkipNextDecrement { get; set; }
    }

    /// <summary>持続効果の保存用の写し。中身は由来のカード定義の Effects[EffectIndex] から復元する。</summary>
    public sealed class PersistentSnapshot
    {
        public int CardId { get; set; }

        /// <summary>強化版の面か</summary>
        public bool Enhanced { get; set; }

        /// <summary>その面の Effects の中の位置</summary>
        public int EffectIndex { get; set; }

        public int RemainingTurns { get; set; }
        public int RemainingCount { get; set; }
        public long GrantOrder { get; set; }
        public bool SkipNextDecrement { get; set; }
    }

    /// <summary>LessonState の保存用の平らな写し（produce-domain.md 6.1）。</summary>
    public sealed class LessonSnapshot
    {
        public LessonKind Kind { get; set; }
        public ParamKind TargetParam { get; set; }
        public int MaxTurns { get; set; }
        public int ClearTarget { get; set; }
        public int PerfectTarget { get; set; }
        public ParamKind[] ExamTurnParams { get; set; } = new ParamKind[0];
        public int[] ExamTurnScoreBonusPercents { get; set; } = new int[0];
        public int ExamEarlyEndScore { get; set; }
        public CardInstanceSnapshot[] Deck { get; set; } = new CardInstanceSnapshot[0];
        public int[] DrawPile { get; set; } = new int[0];
        public int[] Hand { get; set; } = new int[0];
        public int[] Discard { get; set; } = new int[0];
        public int[] Exiled { get; set; } = new int[0];
        public int[] StatusValues { get; set; } = new int[0];
        public bool[] StatusSkipNextDecrement { get; set; } = new bool[0];
        public ParamBonusSnapshot[] ParamBonuses { get; set; } = new ParamBonusSnapshot[0];
        public PersistentSnapshot[] Persistents { get; set; } = new PersistentSnapshot[0];
        public int[] LessonEnhanced { get; set; } = new int[0];
        public int Turn { get; set; }
        public LessonPhase Phase { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Shield { get; set; }
        public int UsesLeft { get; set; }
        public int ParamGained { get; set; }
        public long NextGrantOrder { get; set; }

        /// <summary>
        /// 状態から写しを作る。持続効果の由来は cards から見つける（cards は状態を動かしたエンジンのカタログと同じインスタンスであること）。
        /// </summary>
        public static LessonSnapshot From(LessonState state, ICardCatalog cards)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            if (state.Scheduled.Count > 0)
            {
                throw new NotSupportedException("発動予約は保存できません。");
            }

            var setup = state.Setup;
            var snapshot = new LessonSnapshot
            {
                Kind = setup.Kind,
                TargetParam = setup.TargetParam,
                MaxTurns = setup.MaxTurns,
                ClearTarget = setup.ClearTarget,
                PerfectTarget = setup.PerfectTarget,
                Turn = state.Turn,
                Phase = state.Phase,
                Hp = state.Hp,
                MaxHp = state.MaxHp,
                Shield = state.Shield,
                UsesLeft = state.UsesLeft,
                ParamGained = state.ParamGained,
                NextGrantOrder = state.NextGrantOrder,
            };

            if (setup.Exam != null)
            {
                var turnCount = setup.Exam.Turns.Count;
                snapshot.ExamTurnParams = new ParamKind[turnCount];
                snapshot.ExamTurnScoreBonusPercents = new int[turnCount];
                for (var i = 0; i < turnCount; i++)
                {
                    snapshot.ExamTurnParams[i] = setup.Exam.Turns[i].Param;
                    snapshot.ExamTurnScoreBonusPercents[i] = setup.Exam.Turns[i].ScoreBonusPercent;
                }

                snapshot.ExamEarlyEndScore = setup.Exam.EarlyEndScore;
            }

            snapshot.Deck = CopyCards(state.Deck);
            snapshot.DrawPile = CopyInts(state.Zones.DrawPile);
            snapshot.Hand = CopyInts(state.Zones.Hand);
            snapshot.Discard = CopyInts(state.Zones.Discard);
            snapshot.Exiled = CopyInts(state.Zones.Exiled);

            snapshot.StatusValues = new int[StatusSet.KindCount];
            snapshot.StatusSkipNextDecrement = new bool[StatusSet.KindCount];
            for (var i = 0; i < StatusSet.KindCount; i++)
            {
                snapshot.StatusValues[i] = state.Statuses.Get((StatusKind)i);
                snapshot.StatusSkipNextDecrement[i] = state.Statuses.IsSkipNextDecrement((StatusKind)i);
            }

            snapshot.ParamBonuses = new ParamBonusSnapshot[state.ParamBonuses.Count];
            for (var i = 0; i < snapshot.ParamBonuses.Length; i++)
            {
                var bonus = state.ParamBonuses[i];
                snapshot.ParamBonuses[i] = new ParamBonusSnapshot
                {
                    Percent = bonus.Percent,
                    RemainingTurns = bonus.RemainingTurns,
                    SkipNextDecrement = bonus.SkipNextDecrement,
                };
            }

            snapshot.Persistents = new PersistentSnapshot[state.Persistents.Count];
            for (var i = 0; i < snapshot.Persistents.Length; i++)
            {
                var persistent = state.Persistents[i];
                var snap = FindOrigin(cards, persistent);
                snap.RemainingTurns = persistent.RemainingTurns;
                snap.RemainingCount = persistent.RemainingCount;
                snap.GrantOrder = persistent.GrantOrder;
                snap.SkipNextDecrement = persistent.SkipNextDecrement;
                snapshot.Persistents[i] = snap;
            }

            snapshot.LessonEnhanced = CopyInts(state.LessonEnhanced);
            return snapshot;
        }

        /// <summary>写しから状態を作り直す。復元できない値は ArgumentException。</summary>
        public LessonState ToState(ICardCatalog cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            RequireNotNull(ExamTurnParams, nameof(ExamTurnParams));
            RequireNotNull(ExamTurnScoreBonusPercents, nameof(ExamTurnScoreBonusPercents));
            RequireNotNull(Deck, nameof(Deck));
            RequireNotNull(DrawPile, nameof(DrawPile));
            RequireNotNull(Hand, nameof(Hand));
            RequireNotNull(Discard, nameof(Discard));
            RequireNotNull(Exiled, nameof(Exiled));
            RequireNotNull(StatusValues, nameof(StatusValues));
            RequireNotNull(StatusSkipNextDecrement, nameof(StatusSkipNextDecrement));
            RequireNotNull(ParamBonuses, nameof(ParamBonuses));
            RequireNotNull(Persistents, nameof(Persistents));
            RequireNotNull(LessonEnhanced, nameof(LessonEnhanced));
            RequireNoNullElement(Deck, nameof(Deck));
            RequireNoNullElement(ParamBonuses, nameof(ParamBonuses));
            RequireNoNullElement(Persistents, nameof(Persistents));

            if (StatusValues.Length != StatusSet.KindCount)
            {
                throw new ArgumentException(nameof(StatusValues) + ": 長さが " + StatusSet.KindCount + " ではありません。");
            }

            if (StatusSkipNextDecrement.Length != StatusSet.KindCount)
            {
                throw new ArgumentException(nameof(StatusSkipNextDecrement) + ": 長さが " + StatusSet.KindCount + " ではありません。");
            }

            if (ExamTurnParams.Length != ExamTurnScoreBonusPercents.Length)
            {
                throw new ArgumentException(nameof(ExamTurnScoreBonusPercents) + ": ExamTurnParams と長さが違います。");
            }

            ExamSetup exam = null;
            if (Kind == LessonKind.MidExam || Kind == LessonKind.FinalExam)
            {
                var turns = new ExamTurn[ExamTurnParams.Length];
                for (var i = 0; i < turns.Length; i++)
                {
                    turns[i] = new ExamTurn(ExamTurnParams[i], ExamTurnScoreBonusPercents[i]);
                }

                exam = new ExamSetup(turns, ExamEarlyEndScore);
            }

            var setup = new LessonSetup(Kind, TargetParam, MaxTurns, ClearTarget, PerfectTarget, exam);

            var persistentEffects = new PersistentEffect[Persistents.Length];
            for (var i = 0; i < persistentEffects.Length; i++)
            {
                var p = Persistents[i];
                if (!ContainsCardId(cards, p.CardId))
                {
                    throw new ArgumentException(nameof(Persistents) + ": 未知のカード ID " + p.CardId + " です。");
                }

                var def = cards.Get(p.CardId);
                if (p.Enhanced && def.Enhanced == null)
                {
                    throw new ArgumentException(nameof(Persistents) + ": カード " + p.CardId + " に強化版の面がありません。");
                }

                var face = p.Enhanced ? def.Enhanced : def.Base;
                if (p.EffectIndex < 0 || p.EffectIndex >= face.Effects.Count)
                {
                    throw new ArgumentException(nameof(Persistents) + ": EffectIndex が範囲外です。");
                }

                var effect = face.Effects[p.EffectIndex];
                if (effect.Op != EffectOp.AddPersistent)
                {
                    throw new ArgumentException(nameof(Persistents) + ": EffectIndex の効果が持続効果ではありません。");
                }

                persistentEffects[i] = new PersistentEffect(effect.Trigger, effect.Children, p.RemainingTurns, p.RemainingCount, p.GrantOrder, p.SkipNextDecrement);
            }

            var bonusEntries = new ParamBonusEntry[ParamBonuses.Length];
            for (var i = 0; i < bonusEntries.Length; i++)
            {
                var b = ParamBonuses[i];
                bonusEntries[i] = new ParamBonusEntry(b.Percent, b.RemainingTurns, b.SkipNextDecrement);
            }

            return new LessonState(
                setup,
                ToInstances(Deck),
                new Zones(DrawPile, Hand, Discard, Exiled),
                new StatusSet(StatusValues, StatusSkipNextDecrement),
                bonusEntries,
                new ScheduledEffect[0],
                persistentEffects,
                LessonEnhanced,
                Turn,
                Phase,
                Hp,
                MaxHp,
                Shield,
                UsesLeft,
                ParamGained,
                NextGrantOrder);
        }

        private static bool ContainsCardId(ICardCatalog cards, int cardId)
        {
            var ids = cards.CardIds;
            var low = 0;
            var high = ids.Count - 1;
            while (low <= high)
            {
                var mid = low + (high - low) / 2;
                if (ids[mid] == cardId)
                {
                    return true;
                }

                if (ids[mid] < cardId)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return false;
        }

        internal static CardInstanceSnapshot[] CopyCards(IReadOnlyList<CardInstance> cards)
        {
            var result = new CardInstanceSnapshot[cards.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new CardInstanceSnapshot
                {
                    InstanceId = cards[i].InstanceId,
                    CardId = cards[i].CardId,
                    Enhanced = cards[i].Enhanced,
                };
            }

            return result;
        }

        internal static CardInstance[] ToInstances(CardInstanceSnapshot[] cards)
        {
            var result = new CardInstance[cards.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new CardInstance(cards[i].InstanceId, cards[i].CardId, cards[i].Enhanced);
            }

            return result;
        }

        internal static int[] CopyInts(IEnumerable<int> values)
        {
            return new List<int>(values).ToArray();
        }

        internal static void RequireNotNull(object value, string name)
        {
            if (value == null)
            {
                throw new ArgumentException(name + ": null です。");
            }
        }

        internal static void RequireNoNullElement<T>(T[] values, string name)
            where T : class
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    throw new ArgumentException(name + ": " + i + " 番目の要素が null です。");
                }
            }
        }

        /// <summary>由来のカード・面・効果の位置だけを埋めた PersistentSnapshot を返す。</summary>
        private static PersistentSnapshot FindOrigin(ICardCatalog cards, PersistentEffect persistent)
        {
            foreach (var id in cards.CardIds)
            {
                var def = cards.Get(id);
                for (var face = 0; face < 2; face++)
                {
                    var enhanced = face == 1;
                    var cardFace = enhanced ? def.Enhanced : def.Base;
                    if (cardFace == null)
                    {
                        continue;
                    }

                    for (var i = 0; i < cardFace.Effects.Count; i++)
                    {
                        var effect = cardFace.Effects[i];
                        if (effect.Op == EffectOp.AddPersistent && ReferenceEquals(effect.Children, persistent.Effects))
                        {
                            return new PersistentSnapshot { CardId = id, Enhanced = enhanced, EffectIndex = i };
                        }
                    }
                }
            }

            throw new InvalidOperationException("持続効果の由来のカードが見つかりません。");
        }
    }
}
