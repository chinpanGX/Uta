namespace Uta.Shared.Domain.Rng
{
    /// <summary>
    /// SplitMix64。xoshiro256** の状態の初期化と、ストリームのキーを混ぜるのに使う（produce.md 10.2）。
    /// </summary>
    public static class SplitMix64
    {
        private const ulong Gamma = 0x9E3779B97F4A7C15UL;

        /// <summary>状態を進めて、次の値を返す。</summary>
        public static ulong Next(ref ulong state)
        {
            state = unchecked(state + Gamma);
            return Mix(state);
        }

        /// <summary>1つの値を混ぜる（状態を持たない Next）。</summary>
        public static ulong Hash(ulong value)
        {
            return Mix(unchecked(value + Gamma));
        }

        private static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
