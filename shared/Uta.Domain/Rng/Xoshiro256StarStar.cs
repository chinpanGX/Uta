using System;

namespace Uta.Domain.Rng
{
    /// <summary>
    /// xoshiro256**（repository-layout.md 10.3）。状態（64 ビット × 4）を出し入れでき、保存と再開に使う。
    /// </summary>
    public sealed class Xoshiro256StarStar : IRandom
    {
        private ulong s0;
        private ulong s1;
        private ulong s2;
        private ulong s3;

        public Xoshiro256StarStar(ulong s0, ulong s1, ulong s2, ulong s3)
            : this(s0, s1, s2, s3, 0)
        {
        }

        /// <param name="drawCount">これまでに引いた回数（調査用。結果には影響しない）</param>
        public Xoshiro256StarStar(ulong s0, ulong s1, ulong s2, ulong s3, long drawCount)
        {
            if ((s0 | s1 | s2 | s3) == 0)
            {
                throw new ArgumentException("状態がすべて 0 です。");
            }

            this.s0 = s0;
            this.s1 = s1;
            this.s2 = s2;
            this.s3 = s3;
            DrawCount = drawCount;
        }

        public ulong S0 => s0;
        public ulong S1 => s1;
        public ulong S2 => s2;
        public ulong S3 => s3;

        public long DrawCount { get; private set; }

        /// <summary>SplitMix64 で 64 ビットの値を伸ばして状態にする（xoshiro の作者が推奨する初期化）。</summary>
        public static Xoshiro256StarStar FromSeed(ulong seed)
        {
            var state = seed;
            var a = SplitMix64.Next(ref state);
            var b = SplitMix64.Next(ref state);
            var c = SplitMix64.Next(ref state);
            var d = SplitMix64.Next(ref state);
            return new Xoshiro256StarStar(a, b, c, d);
        }

        public ulong NextUInt64()
        {
            unchecked
            {
                var result = RotateLeft(s1 * 5, 7) * 9;
                var t = s1 << 17;

                s2 ^= s0;
                s3 ^= s1;
                s1 ^= s2;
                s0 ^= s3;
                s2 ^= t;
                s3 = RotateLeft(s3, 45);

                DrawCount++;
                return result;
            }
        }

        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }
    }
}
