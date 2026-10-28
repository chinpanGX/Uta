using System;
using System.Collections.Generic;

namespace Uta.Domain.Rng
{
    /// <summary>
    /// 乱数の取り出し方。どの実装の <see cref="IRandom"/> でも、ここを通して同じ方法で引く（produce.md 10.2）。
    /// </summary>
    public static class RandomOps
    {
        /// <summary>確率の単位（万分率）。</summary>
        public const int RateDenominator = 10000;

        /// <summary>0 以上 n 未満の整数。棄却法で偏りをなくす。</summary>
        public static int NextInt(IRandom random, int n)
        {
            if (n <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n));
            }

            var bound = (ulong)n;
            // 2^64 mod n 未満の値を捨てると、残りは n で割り切れる個数になる
            var threshold = unchecked(0UL - bound) % bound;
            while (true)
            {
                var x = random.NextUInt64();
                if (x >= threshold)
                {
                    return (int)(x % bound);
                }
            }
        }

        /// <summary>確率の判定。率は万分率。率が 0 や 10000 でも必ず1回引く。</summary>
        public static bool Chance(IRandom random, int ratePerTenThousand)
        {
            return NextInt(random, RateDenominator) < ratePerTenThousand;
        }

        /// <summary>min 以上 max 以下の整数。</summary>
        public static int Range(IRandom random, int min, int max)
        {
            if (max < min)
            {
                throw new ArgumentOutOfRangeException(nameof(max));
            }

            return min + NextInt(random, max - min + 1);
        }

        /// <summary>重み付きの抽選。候補は呼び出し側で決まった順（マスタの ID の昇順）に並べて渡す。</summary>
        public static int WeightedIndex(IRandom random, IReadOnlyList<int> weights)
        {
            var total = 0L;
            for (var i = 0; i < weights.Count; i++)
            {
                if (weights[i] < 0)
                {
                    throw new ArgumentException("重みが負です。", nameof(weights));
                }

                total += weights[i];
            }

            if (total <= 0 || total > int.MaxValue)
            {
                throw new ArgumentException("重みの合計が範囲外です。", nameof(weights));
            }

            var r = NextInt(random, (int)total);
            for (var i = 0; i < weights.Count; i++)
            {
                r -= weights[i];
                if (r < 0)
                {
                    return i;
                }
            }

            throw new InvalidOperationException();
        }

        /// <summary>Fisher–Yates。末尾から、i 番目と NextInt(i + 1) 番目を入れ替える。</summary>
        public static void Shuffle<T>(IRandom random, IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = NextInt(random, i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
