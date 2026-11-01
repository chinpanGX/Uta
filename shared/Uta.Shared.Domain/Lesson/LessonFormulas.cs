using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Numerics;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>
    /// 計算式（lesson-card.md 9章）。倍率はパーセントの整数で受け取り、分子と分母を long で掛け合わせて最後に1回だけ丸める。
    /// </summary>
    public static class LessonFormulas
    {
        private const long Percent = 100;

        /// <summary>
        /// 9.1 最終消費量 = ceil( 消費量 × 消費倍率 + 消費体力追加 − 消費体力削減 )。0 未満にはならない。
        /// </summary>
        public static int FinalConsume(int cost, bool costHalf, bool costDouble, int costAdd, int costReduce)
        {
            long numerator;
            long denominator;
            if (costHalf && !costDouble)
            {
                numerator = 1;
                denominator = 2;
            }
            else if (costDouble && !costHalf)
            {
                numerator = 2;
                denominator = 1;
            }
            else
            {
                numerator = 1;
                denominator = 1;
            }

            var value = FixedMath.CeilDiv(cost * numerator + (long)(costAdd - costReduce) * denominator, denominator);
            return FixedMath.ToInt(Math.Max(0, value));
        }

        /// <summary>
        /// 9.2 最終パラメータ上昇値（連撃の1回分）。
        /// </summary>
        /// <param name="cardParam">スキルカードのパラメータ（paramByRatio は ceil(参照値 × 倍率) を渡す）</param>
        /// <param name="focus">集中</param>
        /// <param name="focusBoostPercent">集中強化（%）。仮実装（9.2 の注記）</param>
        /// <param name="focusScalePercent">集中効果の倍率（%）。既定 100</param>
        /// <param name="goodConditionTurns">好調のターン数</param>
        /// <param name="excellentTurns">絶好調のターン数</param>
        /// <param name="goodScalePercent">好調効果の倍率（%）。既定 100</param>
        /// <param name="bonusPercents">上昇量増加の % の並び（総和を使う）</param>
        /// <param name="malusPercent">上昇量減少の総和（%。負の値）</param>
        public static int ParamGain(
            int cardParam,
            int focus,
            int focusBoostPercent,
            int focusScalePercent,
            int goodConditionTurns,
            int excellentTurns,
            int goodScalePercent,
            IReadOnlyList<int> bonusPercents,
            int malusPercent)
        {
            // カード値 = パラメータ + 集中 × (1 + 集中強化) × 集中の倍率。分母 100 × 100
            var cardNumerator = cardParam * Percent * Percent
                + (long)focus * (Percent + focusBoostPercent) * focusScalePercent;
            var cardDenominator = Percent * Percent;

            // 好調倍率。分母 100 × 100
            long goodNumerator;
            if (goodConditionTurns <= 0)
            {
                goodNumerator = Percent * Percent;
            }
            else if (excellentTurns <= 0)
            {
                goodNumerator = Percent * Percent + 50L * goodScalePercent;
            }
            else
            {
                goodNumerator = Percent * Percent + (50L + 10L * goodConditionTurns) * goodScalePercent;
            }

            var goodDenominator = Percent * Percent;

            var bonusSum = 0L;
            if (bonusPercents != null)
            {
                for (var i = 0; i < bonusPercents.Count; i++)
                {
                    bonusSum += bonusPercents[i];
                }
            }

            // 分母は 10^12。分子はパラメータ 1,000・倍率 300% 程度でも 10^16 に収まる。桁あふれは checked で検出する
            var numerator = checked(cardNumerator * goodNumerator * (Percent + bonusSum) * (Percent + malusPercent));
            var denominator = cardDenominator * goodDenominator * Percent * Percent;
            return FixedMath.ToInt(Math.Max(0, FixedMath.CeilDiv(numerator, denominator)));
        }

        /// <summary>paramByRatio の「スキルカードのパラメータ」= ceil(参照値 × 倍率)。</summary>
        public static int RatioParam(int sourceValue, int percent)
        {
            return FixedMath.ToInt(FixedMath.CeilDiv((long)sourceValue * percent, Percent));
        }

        /// <summary>9.3 元気増加量 = 効果の値 + やる気。元気増加無効中は 0。</summary>
        public static int ShieldGain(int value, int motivation, bool shieldGainBlocked)
        {
            return shieldGainBlocked ? 0 : Math.Max(0, value + motivation);
        }

        /// <summary>固定元気の増加量。やる気を足さない。元気増加無効中は 0（lesson-card.md 9.3）。</summary>
        public static int FixedShieldGain(int value, bool shieldGainBlocked)
        {
            return shieldGainBlocked ? 0 : Math.Max(0, value);
        }

        /// <summary>元気を percent% にする（端数は切り捨て。lesson-card.md 7.2）。</summary>
        public static int ScaledShield(int shield, int percent)
        {
            return FixedMath.ToInt(FixedMath.FloorDiv((long)shield * percent, Percent));
        }

        /// <summary>9.4 好印象による上昇値 = ceil( 好印象 × (1 + 好印象強化) × (1 + 上昇量増加の総和) )。好調倍率は乗せない。</summary>
        public static int ImpressionGain(int impression, int impressionBoostPercent, IReadOnlyList<int> bonusPercents)
        {
            var bonusSum = 0L;
            if (bonusPercents != null)
            {
                for (var i = 0; i < bonusPercents.Count; i++)
                {
                    bonusSum += bonusPercents[i];
                }
            }

            var numerator = impression * (Percent + impressionBoostPercent) * (Percent + bonusSum);
            return FixedMath.ToInt(Math.Max(0, FixedMath.CeilDiv(numerator, Percent * Percent)));
        }
    }
}
