using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>
    /// 2 つの育成の状態を項目ごとに比べる。再生のテストと、IL2CPP での再生の確認から使う。
    /// 形は <see cref="LessonStateComparer"/> と同じ。
    /// </summary>
    public static class ProduceStateComparer
    {
        private static readonly ParamKind[] ParamOrder = { ParamKind.Singing, ParamKind.Rhythm, ParamKind.Expression };

        /// <summary>状態の全部の値を「項目=値」の文字列の並びにする。</summary>
        public static IReadOnlyList<string> Describe(ProduceState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var lines = new List<string>();
            var input = state.Input;

            lines.Add("scenario=" + Text(input.ScenarioId));
            lines.Add("idol=" + Text(input.IdolId));
            lines.Add("playerLv=" + Text(input.PlayerLevel));
            lines.Add("supports=" + string.Join(
                ",",
                input.SupportCards.Select(s => Text(s.CardId) + ":" + Text(s.Level) + ":" + Text(s.LimitBreak))));

            lines.Add("phase=" + Text((int)state.Phase));
            lines.Add("week=" + Text(state.Week));
            lines.Add("attempt=" + Text(state.Attempt));
            lines.Add("nextOperation=" + Text(state.NextOperationNumber));
            lines.Add("nextInstanceId=" + Text(state.NextInstanceId));
            lines.Add("retryLeft=" + Text(state.ExamRetryLeft));

            lines.Add("singing=" + Text(state.GetParam(ParamKind.Singing)));
            lines.Add("rhythm=" + Text(state.GetParam(ParamKind.Rhythm)));
            lines.Add("expression=" + Text(state.GetParam(ParamKind.Expression)));
            lines.Add("hp=" + Text(state.Hp));
            lines.Add("maxHp=" + Text(state.MaxHp));

            lines.Add("bonus=" + string.Join(",", ParamOrder.Select(p => Text(state.GetBonusPerThousand(p)))));
            lines.Add("deck=" + Deck(state.Deck));
            lines.Add("classParams=" + Ids(state.ClassOptionParams));
            lines.Add("offer=" + Ids(state.OfferCards));

            if (state.ExamBackup == null)
            {
                lines.Add("backup=none");
            }
            else
            {
                var backup = state.ExamBackup;
                lines.Add("backup=" + Ids(backup.Parameters) + "," + Text(backup.Hp) + "," + Text(backup.NextInstanceId) + ";" + Deck(backup.Deck));
            }

            lines.Add("lastExam=" + Report(state.LastExam));
            lines.Add("midExam=" + Report(state.MidExam));

            if (state.Result == null)
            {
                lines.Add("result=none");
            }
            else
            {
                var result = state.Result;
                lines.Add("result=" + Text((int)result.Outcome) + "," + Text(result.EvaluationValue) + "," + Text((int)result.Rank)
                    + "," + Text(result.FinalScore) + "," + Text(result.FinalRank));
            }

            lines.Add("operations=" + Text(state.Operations.Count));
            for (var i = 0; i < state.Operations.Count; i++)
            {
                var op = state.Operations[i];
                lines.Add("operation." + Text(i) + "="
                    + Text(op.Number) + "," + Text((int)op.Command.Kind) + "," + Text(op.Command.Arg1) + "," + Text(op.Command.Arg2)
                    + "," + Text(op.Command.Arg3) + "," + Text(op.Week) + "," + Text(op.Attempt) + "," + Text(op.Turn));
            }

            if (state.Lesson == null)
            {
                lines.Add("lesson=none");
            }
            else
            {
                var lessonLines = LessonStateComparer.Describe(state.Lesson);
                for (var i = 0; i < lessonLines.Count; i++)
                {
                    lines.Add("lesson." + lessonLines[i]);
                }
            }

            return lines;
        }

        /// <summary>食い違った項目だけを返す。同じなら空。</summary>
        public static IReadOnlyList<string> Diff(ProduceState expected, ProduceState actual)
        {
            var expectedLines = Describe(expected);
            var actualLines = Describe(actual);
            var count = Math.Max(expectedLines.Count, actualLines.Count);
            var differences = new List<string>();

            for (var i = 0; i < count; i++)
            {
                var e = i < expectedLines.Count ? expectedLines[i] : "(なし)";
                var a = i < actualLines.Count ? actualLines[i] : "(なし)";
                if (!string.Equals(e, a, StringComparison.Ordinal))
                {
                    differences.Add("期待 " + e + " / 実際 " + a);
                }
            }

            return differences;
        }

        private static string Text(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Ids(IEnumerable<int> ids)
        {
            return string.Join(",", ids.Select(Text));
        }

        private static string Deck(IEnumerable<CardInstance> deck)
        {
            return string.Join(
                ",",
                deck.Select(c => Text(c.InstanceId) + ":" + Text(c.CardId) + ":" + (c.Enhanced ? "1" : "0")));
        }

        private static string Report(ExamReport report)
        {
            if (report == null)
            {
                return "none";
            }

            return Text((int)report.Kind) + "," + Text(report.Attempt) + "," + Text(report.Score) + "," + Text(report.Rank)
                + "," + (report.Passed ? "1" : "0") + ";" + Ids(report.NpcScores);
        }
    }
}
