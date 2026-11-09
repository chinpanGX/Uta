using System;

namespace Uta.Client.Application.Support
{
    /// <summary>上限解放の結果。Succeeded のときだけ LimitBreak・MaxLevel に値が入る。</summary>
    public sealed class LimitBreakResult
    {
        private LimitBreakResult(SupportResultKind kind, int limitBreak, int maxLevel)
        {
            Kind = kind;
            LimitBreak = limitBreak;
            MaxLevel = maxLevel;
        }

        public SupportResultKind Kind { get; }

        /// <summary>Succeeded のとき解放後の回数。それ以外は 0。</summary>
        public int LimitBreak { get; }

        /// <summary>Succeeded のとき解放後の最大レベル。それ以外は 0。</summary>
        public int MaxLevel { get; }

        public static LimitBreakResult Success(int limitBreak, int maxLevel)
        {
            return new LimitBreakResult(SupportResultKind.Succeeded, limitBreak, maxLevel);
        }

        public static LimitBreakResult Rejected(SupportResultKind kind)
        {
            if (kind == SupportResultKind.Succeeded)
            {
                throw new ArgumentException("kind must not be Succeeded", nameof(kind));
            }

            return new LimitBreakResult(kind, 0, 0);
        }
    }
}
