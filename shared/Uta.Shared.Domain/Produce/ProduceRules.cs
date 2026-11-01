using System;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>育成のルールの定数（produce.md・idol-support.md）。</summary>
    public static class ProduceRules
    {
        private static readonly int[] NpcPercents = { 80, 65, 50, 35, 20 };

        /// <summary>パラメータの数（歌唱・律動・表現）</summary>
        public const int ParamCount = 3;

        /// <summary>スキルカードの選択肢の枠数（produce.md 3.5）</summary>
        public const int OfferSlotCount = 3;

        /// <summary>休む（produce.md 3.3）</summary>
        public const int RestRecoverPercent = 70;

        /// <summary>仕上げレッスンの後（produce.md 4.2）</summary>
        public const int FinishingRecoverPercent = 70;

        /// <summary>パーフェクトで終わったとき、残りターン 1 つあたりの回復（produce.md 4.3）</summary>
        public const int PerfectRecoverPerRemainingTurn = 2;

        /// <summary>3 位以内で合格（produce.md 5.1）</summary>
        public const int PassRank = 3;

        public const int NpcCount = 5;

        /// <summary>サポートカードの最大枚数（idol-support.md 2.1）</summary>
        public const int MaxSupportCards = 5;

        public const int MaxLimitBreak = 4;

        /// <summary>produce.md 6章のパラメータ分の倍率（2.3）</summary>
        public const int ParamEvaluationPerThousand = 2300;

        /// <summary>NPC の得点の割合。0..4 → 80, 65, 50, 35, 20。範囲外は ArgumentOutOfRangeException</summary>
        public static int NpcPercent(int index)
        {
            if (index < 0 || index >= NpcPercents.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return NpcPercents[index];
        }

        /// <summary>最終試験の順位の評価値。1 → 1700、2 → 900、3 → 500、それ以外（0 を含む）→ 0</summary>
        public static int FinalRankScore(int rank)
        {
            switch (rank)
            {
                case 1:
                    return 1700;
                case 2:
                    return 900;
                case 3:
                    return 500;
                default:
                    return 0;
            }
        }
    }

    /// <summary>パラメータの添字（歌唱 0・律動 1・表現 2）。</summary>
    public static class ProduceParams
    {
        public static int ToIndex(ParamKind kind)
        {
            switch (kind)
            {
                case ParamKind.Singing:
                    return 0;
                case ParamKind.Rhythm:
                    return 1;
                case ParamKind.Expression:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static ParamKind FromIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return ParamKind.Singing;
                case 1:
                    return ParamKind.Rhythm;
                case 2:
                    return ParamKind.Expression;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>歌唱・律動・表現のどれかのとき真。</summary>
        public static bool IsValid(ParamKind kind)
        {
            return kind == ParamKind.Singing || kind == ParamKind.Rhythm || kind == ParamKind.Expression;
        }
    }
}
