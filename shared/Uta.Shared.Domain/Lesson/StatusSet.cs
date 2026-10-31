using System;
using System.Collections.Generic;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>
    /// ステータスの値（lesson-card.md 8章）と、ターン制ステータスの数え方（11章）。
    /// </summary>
    public sealed class StatusSet
    {
        /// <summary>StatusKind の数。</summary>
        public const int KindCount = 12;

        private readonly int[] values = new int[KindCount];
        private readonly bool[] skipNextDecrement = new bool[KindCount];

        public StatusSet()
        {
        }

        /// <summary>保存した状態から作り直す。並びは StatusKind の数値の順。</summary>
        public StatusSet(IReadOnlyList<int> values, IReadOnlyList<bool> skipNextDecrement)
        {
            if (values == null || values.Count != KindCount)
            {
                throw new ArgumentException("値の数が合いません。", nameof(values));
            }

            if (skipNextDecrement == null || skipNextDecrement.Count != KindCount)
            {
                throw new ArgumentException("値の数が合いません。", nameof(skipNextDecrement));
            }

            for (var i = 0; i < KindCount; i++)
            {
                if (values[i] < 0)
                {
                    throw new ArgumentException("値が負です。", nameof(values));
                }

                this.values[i] = values[i];
                this.skipNextDecrement[i] = skipNextDecrement[i];
            }
        }

        public int Get(StatusKind kind)
        {
            return values[(int)kind];
        }

        /// <summary>このターンの終了時に減らさないか（プレイヤー操作中に初めて付与した）。</summary>
        public bool IsSkipNextDecrement(StatusKind kind)
        {
            return skipNextDecrement[(int)kind];
        }

        /// <summary>ターン数で持つステータスか。ターン終了時（E5）に1減る。</summary>
        public static bool IsTurnBased(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.GoodCondition:
                case StatusKind.Excellent:
                case StatusKind.CostHalf:
                case StatusKind.CostDouble:
                case StatusKind.GenkiBlock:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>値を足す。0 未満にはならない。</summary>
        internal void Add(StatusKind kind, int amount, LessonPhase phase)
        {
            var index = (int)kind;
            var before = values[index];
            values[index] = Math.Max(0, before + amount);

            // 11章: プレイヤー操作中に初めて付与したものは、そのターンの終了時に減らない
            if (IsTurnBased(kind) && before == 0 && values[index] > 0 && phase == LessonPhase.Player)
            {
                skipNextDecrement[index] = true;
            }
        }

        /// <summary>ターン終了時（E5）。ターン制のステータスを1減らす。</summary>
        internal void DecrementTurns()
        {
            for (var i = 0; i < KindCount; i++)
            {
                if (!IsTurnBased((StatusKind)i))
                {
                    continue;
                }

                if (skipNextDecrement[i])
                {
                    skipNextDecrement[i] = false;
                }
                else if (values[i] > 0)
                {
                    values[i]--;
                }
            }
        }
    }
}
