using System;
using System.Collections.Generic;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>試験の 1 ターン分の設定（審査のパラメータと最終スコアボーナス。lesson-card.md 9.5）。</summary>
    public readonly struct ExamTurn
    {
        public ExamTurn(ParamKind param, int scoreBonusPercent)
        {
            if (param == ParamKind.Nothing)
            {
                throw new ArgumentOutOfRangeException(nameof(param));
            }

            if (scoreBonusPercent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scoreBonusPercent));
            }

            Param = param;
            ScoreBonusPercent = scoreBonusPercent;
        }

        public ParamKind Param { get; }
        public int ScoreBonusPercent { get; }
    }

    /// <summary>試験の設定（lesson-domain-types.md 2章）。</summary>
    public sealed class ExamSetup
    {
        public ExamSetup(IReadOnlyList<ExamTurn> turns, int earlyEndScore)
        {
            if (turns == null)
            {
                throw new ArgumentNullException(nameof(turns));
            }

            if (turns.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(turns));
            }

            if (earlyEndScore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(earlyEndScore));
            }

            var copy = new ExamTurn[turns.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = turns[i];
            }

            Turns = copy;
            EarlyEndScore = earlyEndScore;
        }

        public IReadOnlyList<ExamTurn> Turns { get; }

        /// <summary>0 は早期終了なし</summary>
        public int EarlyEndScore { get; }
    }
}
