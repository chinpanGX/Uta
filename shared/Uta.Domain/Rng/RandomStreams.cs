using System;

namespace Uta.Domain.Rng
{
    /// <summary>乱数ストリームの用途（produce.md 10.2）。値はストリームの初期化に使うので変えない。</summary>
    public enum RandomStreamKind
    {
        Scenario = 0,
        Week = 1,
        Offer = 2,
        Lesson = 3,
    }

    /// <summary>ストリームを区別するキー。用途・週番号・挑戦回数。</summary>
    public readonly struct RandomStreamKey : IEquatable<RandomStreamKey>
    {
        public RandomStreamKey(RandomStreamKind kind, int week, int attempt)
        {
            if (week < 0 || week > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(week));
            }

            if (attempt < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attempt));
            }

            Kind = kind;
            Week = week;
            Attempt = attempt;
        }

        public RandomStreamKind Kind { get; }
        public int Week { get; }
        public int Attempt { get; }

        /// <summary>(種類 &lt;&lt; 48) | (週 &lt;&lt; 32) | 挑戦回数</summary>
        public ulong ToUInt64()
        {
            return ((ulong)Kind << 48) | ((ulong)Week << 32) | (uint)Attempt;
        }

        public bool Equals(RandomStreamKey other)
        {
            return Kind == other.Kind && Week == other.Week && Attempt == other.Attempt;
        }

        public override bool Equals(object obj)
        {
            return obj is RandomStreamKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ToUInt64().GetHashCode();
        }
    }

    public static class RandomStreams
    {
        /// <summary>マスターシードとキーから、そのストリームの乱数を作る。</summary>
        public static Xoshiro256StarStar Create(ulong masterSeed, RandomStreamKey key)
        {
            return Xoshiro256StarStar.FromSeed(masterSeed ^ SplitMix64.Hash(key.ToUInt64()));
        }
    }
}
