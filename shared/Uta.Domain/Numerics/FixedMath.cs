using System;

namespace Uta.Domain.Numerics
{
    /// <summary>
    /// 整数の固定小数点の計算。float / double を使わずに、丸めを1か所で決める（lesson-card.md 9.6）。
    /// </summary>
    public static class FixedMath
    {
        /// <summary>切り上げの除算。分母は正。</summary>
        public static long CeilDiv(long numerator, long denominator)
        {
            if (denominator <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            var quotient = numerator / denominator;
            if (numerator % denominator > 0)
            {
                quotient++;
            }

            return quotient;
        }

        /// <summary>切り捨ての除算（負の数は小さい方へ）。分母は正。</summary>
        public static long FloorDiv(long numerator, long denominator)
        {
            if (denominator <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            var quotient = numerator / denominator;
            if (numerator % denominator < 0)
            {
                quotient--;
            }

            return quotient;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        /// <summary>long を int に収める。範囲外は例外にする（計算の桁あふれを見逃さない）。</summary>
        public static int ToInt(long value)
        {
            if (value < int.MinValue || value > int.MaxValue)
            {
                throw new OverflowException("値が int の範囲を超えました: " + value);
            }

            return (int)value;
        }
    }
}
