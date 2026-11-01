using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>
    /// 2 つのレッスンの状態を項目ごとに比べる（lesson-phases.md 8章）。
    /// 再生のテストと、IL2CPP での再生の確認から使う。
    /// </summary>
    public static class LessonStateComparer
    {
        /// <summary>状態の全部の値を「項目=値」の文字列の並びにする。</summary>
        public static IReadOnlyList<string> Describe(LessonState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var lines = new List<string>();
            var setup = state.Setup;

            lines.Add("setup=" + Join(
                (int)setup.Kind,
                (int)setup.TargetParam,
                setup.MaxTurns,
                setup.ClearTarget,
                setup.PerfectTarget));

            if (setup.Exam == null)
            {
                lines.Add("exam=none");
            }
            else
            {
                lines.Add("exam=" + Text(setup.Exam.EarlyEndScore) + ";"
                    + string.Join(",", setup.Exam.Turns.Select(t => Text((int)t.Param) + ":" + Text(t.ScoreBonusPercent))));
            }

            lines.Add("turn=" + Text(state.Turn));
            lines.Add("phase=" + Text((int)state.Phase));
            lines.Add("hp=" + Text(state.Hp));
            lines.Add("maxHp=" + Text(state.MaxHp));
            lines.Add("shield=" + Text(state.Shield));
            lines.Add("usesLeft=" + Text(state.UsesLeft));
            lines.Add("paramGained=" + Text(state.ParamGained));
            lines.Add("nextGrantOrder=" + Text(state.NextGrantOrder));

            lines.Add("deck=" + string.Join(
                ",",
                state.Deck.Select(c => Text(c.InstanceId) + ":" + Text(c.CardId) + ":" + (c.Enhanced ? "1" : "0"))));

            lines.Add("drawPile=" + Ids(state.Zones.DrawPile));
            lines.Add("hand=" + Ids(state.Zones.Hand));
            lines.Add("discard=" + Ids(state.Zones.Discard));
            lines.Add("exiled=" + Ids(state.Zones.Exiled));
            lines.Add("lessonEnhanced=" + Ids(state.LessonEnhanced));

            for (var kind = 1; kind < StatusSet.KindCount; kind++)
            {
                var statusKind = (StatusKind)kind;
                lines.Add("status." + Text(kind) + "="
                    + Text(state.Statuses.Get(statusKind)) + "/"
                    + (state.Statuses.IsSkipNextDecrement(statusKind) ? "1" : "0"));
            }

            for (var i = 0; i < state.ParamBonuses.Count; i++)
            {
                var bonus = state.ParamBonuses[i];
                lines.Add("paramBonus." + Text(i) + "=" + Join(bonus.Percent, bonus.RemainingTurns, bonus.SkipNextDecrement ? 1 : 0));
            }

            for (var i = 0; i < state.Scheduled.Count; i++)
            {
                var scheduled = state.Scheduled[i];
                lines.Add("scheduled." + Text(i) + "=" + Join(scheduled.RemainingTurns, scheduled.GrantOrder) + "," + Ops(scheduled.Effects));
            }

            for (var i = 0; i < state.Persistents.Count; i++)
            {
                var persistent = state.Persistents[i];
                lines.Add("persistent." + Text(i) + "="
                    + Join((int)persistent.Trigger, persistent.RemainingTurns, persistent.RemainingCount, persistent.GrantOrder, persistent.SkipNextDecrement ? 1 : 0)
                    + "," + Ops(persistent.Effects));
            }

            return lines;
        }

        /// <summary>食い違った項目だけを返す。同じなら空。</summary>
        public static IReadOnlyList<string> Diff(LessonState expected, LessonState actual)
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

        private static string Text(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Join(params long[] values)
        {
            return string.Join(",", values.Select(Text));
        }

        private static string Ids(IEnumerable<int> ids)
        {
            return string.Join(",", ids.Select(Text));
        }

        private static string Ops(IReadOnlyList<EffectDef> effects)
        {
            return string.Join(";", effects.Select(e => Text((int)e.Op)));
        }
    }
}
