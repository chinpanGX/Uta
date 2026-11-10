using System;

namespace Uta.Client.Application.Support
{
    /// <summary>レベル上げの結果。Succeeded のときだけ Level・SupportPoints・Gold に値が入る。</summary>
    public sealed class LevelUpResult
    {
        private LevelUpResult(SupportResultKind kind, int level, int supportPoints, int gold)
        {
            Kind = kind;
            Level = level;
            SupportPoints = supportPoints;
            Gold = gold;
        }

        public SupportResultKind Kind { get; }

        /// <summary>Succeeded のとき上げた後のレベル。それ以外は 0。</summary>
        public int Level { get; }

        /// <summary>Succeeded のとき残りの強化pt。それ以外は 0。</summary>
        public int SupportPoints { get; }

        /// <summary>Succeeded のとき残りのゴールド。それ以外は 0。</summary>
        public int Gold { get; }

        public static LevelUpResult Success(int level, int supportPoints, int gold)
        {
            return new LevelUpResult(SupportResultKind.Succeeded, level, supportPoints, gold);
        }

        public static LevelUpResult Rejected(SupportResultKind kind)
        {
            if (kind == SupportResultKind.Succeeded)
            {
                throw new ArgumentException("kind must not be Succeeded", nameof(kind));
            }

            return new LevelUpResult(kind, 0, 0, 0);
        }
    }
}
