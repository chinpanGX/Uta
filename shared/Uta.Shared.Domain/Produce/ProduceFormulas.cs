using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Numerics;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>評価値の内訳（produce.md 6章）。</summary>
    public readonly struct EvaluationResult
    {
        public EvaluationResult(int paramPart, int rankPart, int scorePart, ProduceRank rank)
        {
            ParamPart = paramPart;
            RankPart = rankPart;
            ScorePart = scorePart;
            Rank = rank;
        }

        /// <summary>パラメータ分</summary>
        public int ParamPart { get; }

        /// <summary>最終試験の順位分</summary>
        public int RankPart { get; }

        /// <summary>最終試験の得点分</summary>
        public int ScorePart { get; }

        /// <summary>評価値（3 つの合計）</summary>
        public int Value => ParamPart + RankPart + ScorePart;

        /// <summary>評価ランク</summary>
        public ProduceRank Rank { get; }
    }

    /// <summary>育成の計算式（produce.md）。整数だけで計算する。</summary>
    public static class ProduceFormulas
    {
        /// <summary>上限で止める加算（produce.md 4.3）。amount が負なら ArgumentOutOfRangeException。</summary>
        public static int AddParam(int current, int amount, int cap)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return (int)Math.Min((long)cap, (long)current + amount);
        }

        /// <summary>最大体力で止める回復。amount が負なら ArgumentOutOfRangeException。</summary>
        public static int Heal(int hp, int amount, int maxHp)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return (int)Math.Min((long)maxHp, (long)hp + amount);
        }

        /// <summary>休むときの回復量。最大体力の 70%（切り捨て）。</summary>
        public static int RestAmount(int maxHp)
        {
            return FixedMath.ToInt((long)maxHp * ProduceRules.RestRecoverPercent / 100);
        }

        /// <summary>ボーナス分の加算量。floor(amount * bonus / 1000)。どちらかが負なら ArgumentOutOfRangeException。</summary>
        public static int BonusAmount(int amount, int bonusPerThousand)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (bonusPerThousand < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bonusPerThousand));
            }

            return FixedMath.ToInt((long)amount * bonusPerThousand / 1000);
        }

        /// <summary>サポート能力の値（idol-support.md 2.3）。</summary>
        public static int AbilityValue(int valueAtLv1, int valueAtMax, int level, int maxLevel)
        {
            if (maxLevel < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevel));
            }

            if (level < 1 || level > maxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (valueAtMax < valueAtLv1)
            {
                throw new ArgumentOutOfRangeException(nameof(valueAtMax));
            }

            var delta = (long)(valueAtMax - valueAtLv1) * (level - 1) / (maxLevel - 1);
            return FixedMath.ToInt(valueAtLv1 + delta);
        }

        /// <summary>レッスンで得たパラメータの配分（produce.md 4.3 の 1）。長さ 3 の配列（添字は ProduceParams.ToIndex）。</summary>
        public static int[] DistributeGain(bool finishing, ParamKind target, int gained, int clearTarget, ParamKind examFirstParam)
        {
            if (gained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gained));
            }

            var result = new int[ProduceRules.ParamCount];
            var targetIndex = ProduceParams.ToIndex(target);
            var firstIndex = ProduceParams.ToIndex(examFirstParam);
            if (!finishing)
            {
                result[targetIndex] = gained;
                return result;
            }

            var toTarget = Math.Min(gained, clearTarget);
            var excess = gained - toTarget;
            var each = excess / ProduceRules.ParamCount;
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = each;
            }

            result[targetIndex] += toTarget;
            result[firstIndex] += excess % ProduceRules.ParamCount;
            return result;
        }

        /// <summary>パーフェクトで終わったときの体力の回復量。max(0, maxTurns − lastTurn) * 2。</summary>
        public static int PerfectRecover(int maxTurns, int lastTurn)
        {
            return Math.Max(0, maxTurns - lastTurn) * ProduceRules.PerfectRecoverPerRemainingTurn;
        }

        /// <summary>審査の得点ボーナス（%）。produce.md 5.2。rank が 1〜3 以外は ArgumentOutOfRangeException。</summary>
        public static int ScoreBonusPercent(ProduceCatalog catalog, int paramValue, int rank)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (rank < 1 || rank > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            var a = (long)catalog.ScoreBonusA * paramValue / 1000;
            return FixedMath.ToInt(a + catalog.GetScoreBonusB(rank));
        }

        /// <summary>NPC の得点。長さ 5。</summary>
        public static IReadOnlyList<int> NpcScores(int rivalBaseScore)
        {
            var scores = new int[ProduceRules.NpcCount];
            for (var i = 0; i < scores.Length; i++)
            {
                scores[i] = FixedMath.ToInt((long)rivalBaseScore * ProduceRules.NpcPercent(i) / 100);
            }

            return scores;
        }

        /// <summary>試験の順位。1 + (score より大きい NPC の得点の数)。同点はプレイヤーが上。</summary>
        public static int ExamRank(int score, IReadOnlyList<int> npcScores)
        {
            var rank = 1;
            for (var i = 0; i < npcScores.Count; i++)
            {
                if (npcScores[i] > score)
                {
                    rank++;
                }
            }

            return rank;
        }

        /// <summary>評価値とランク（produce.md 6章）。finalRank が 0 は最終試験を受けていない。</summary>
        public static EvaluationResult Evaluate(ProduceCatalog catalog, int paramSum, int finalRank, int finalScore)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (paramSum < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paramSum));
            }

            if (finalRank < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finalRank));
            }

            if (finalScore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finalScore));
            }

            var paramPart = FixedMath.ToInt((long)paramSum * ProduceRules.ParamEvaluationPerThousand / 1000);
            var rankPart = ProduceRules.FinalRankScore(finalRank);

            var scorePart = 0;
            var bands = catalog.EvaluationScoreBands;
            var bandIndex = -1;
            for (var i = 0; i < bands.Count; i++)
            {
                if (bands[i].MinScore <= finalScore && (bandIndex < 0 || bands[i].MinScore > bands[bandIndex].MinScore))
                {
                    bandIndex = i;
                }
            }

            if (bandIndex >= 0)
            {
                var band = bands[bandIndex];
                scorePart = FixedMath.ToInt((long)finalScore * band.ScalePerThousand / 1000 + band.Fixed);
            }

            var value = (long)paramPart + rankPart + scorePart;
            var rank = ProduceRank.F;
            var rankMin = -1;
            var ranks = catalog.EvaluationRanks;
            for (var i = 0; i < ranks.Count; i++)
            {
                if (ranks[i].MinValue <= value && ranks[i].MinValue > rankMin)
                {
                    rankMin = ranks[i].MinValue;
                    rank = ranks[i].Rank;
                }
            }

            return new EvaluationResult(paramPart, rankPart, scorePart, rank);
        }
    }
}
