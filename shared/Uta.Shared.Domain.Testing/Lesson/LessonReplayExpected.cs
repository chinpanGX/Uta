using System;
using System.Collections.Generic;
using System.Text;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// <see cref="LessonReplayScenario"/> の最後の状態を .NET で再生した値（LessonStateComparer.Describe の行）。
    /// IL2CPP のプレイヤーのテストがこれと比べる。値は LessonReplayExpectedTests の失敗の出力から写す（手で書かない）。
    /// </summary>
    public static class LessonReplayExpected
    {
        public static IReadOnlyList<string> Normal { get; } = new[]
        {
            "setup=1,2,7,1000,2000",
            "exam=none",
            "turn=7",
            "phase=5",
            "hp=11",
            "maxHp=30",
            "shield=1",
            "usesLeft=0",
            "paramGained=76",
            "nextGrantOrder=1",
            "deck=1:1:0,2:2:0,3:3:0,4:4:0,5:5:0,6:6:0,7:201:0,8:202:0,9:204:0,10:207:0,11:210:0,12:214:0,13:215:0,14:7:0,15:203:0,16:205:0,17:206:0,18:208:0,19:209:0,20:211:0,21:212:0,22:213:0,23:101:0,24:104:0,25:106:0,26:107:0,27:111:0,28:102:0,29:103:0,30:105:0,31:108:0,32:109:0,33:110:0,34:112:0,35:113:0,36:114:0",
            "drawPile=36,19,14,22,17,6,15,33,20,5,16,4,13,8,26",
            "hand=",
            "discard=34,23,29,28,10,11,32,7,24,1,2,27,31,30,35,3,25",
            "exiled=21,12,9,18",
            "lessonEnhanced=",
            "status.1=0/0",
            "status.2=0/0",
            "status.3=0/0",
            "status.4=0/0",
            "status.5=9/0",
            "status.6=0/0",
            "status.7=6/0",
            "status.8=0/0",
            "status.9=0/0",
            "status.10=0/0",
            "status.11=0/0",
            "status.12=0/0",
            "persistent.0=8,-1,-1,0,0,11",
        };

        public static IReadOnlyList<string> Exam { get; } = new[]
        {
            "setup=4,0,7,0,0",
            "exam=300;1:100,2:120,3:150,1:200,2:100,3:150,1:200",
            "turn=7",
            "phase=5",
            "hp=21",
            "maxHp=40",
            "shield=9",
            "usesLeft=0",
            "paramGained=106",
            "nextGrantOrder=1",
            "deck=1:1:0,2:2:0,3:3:0,4:4:0,5:5:0,6:6:0,7:201:0,8:202:0,9:204:0,10:207:0,11:210:0,12:214:0,13:215:0,14:7:0,15:203:0,16:205:0,17:206:0,18:208:0,19:209:0,20:211:0,21:212:0,22:213:0,23:101:0,24:104:0,25:106:0,26:107:0,27:111:0,28:102:0,29:103:0,30:105:0,31:108:0,32:109:0,33:110:0,34:112:0,35:113:0,36:114:0",
            "drawPile=15,1,24,20,7,22,29,8,5,31,10,33,14,36,3",
            "hand=",
            "discard=34,9,26,35,27,32,28,2,6,30,25,19,18,23,16,4",
            "exiled=21,13,11,12,17",
            "lessonEnhanced=",
            "status.1=2/0",
            "status.2=0/0",
            "status.3=2/0",
            "status.4=0/0",
            "status.5=7/0",
            "status.6=0/0",
            "status.7=3/0",
            "status.8=0/0",
            "status.9=0/0",
            "status.10=0/0",
            "status.11=0/0",
            "status.12=0/0",
            "persistent.0=8,-1,-1,0,0,11",
        };

        /// <summary>
        /// 期待値の行と状態を項目ごとに比べ、食い違った項目だけを返す。同じなら空。
        /// LessonStateComparer.Diff は状態どうしを比べるので、期待値が文字列のときはこちらを使う。
        /// </summary>
        public static IReadOnlyList<string> Compare(IReadOnlyList<string> expected, LessonState actual)
        {
            if (expected == null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            var actualLines = LessonStateComparer.Describe(actual);
            var count = Math.Max(expected.Count, actualLines.Count);
            var differences = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var e = i < expected.Count ? expected[i] : "(なし)";
                var a = i < actualLines.Count ? actualLines[i] : "(なし)";
                if (!string.Equals(e, a, StringComparison.Ordinal))
                {
                    differences.Add("[" + i + "] 期待 " + e + " / 実際 " + a);
                }
            }

            return differences;
        }

        /// <summary>状態を、このファイルに写せる C# の文字列の並びにする（期待値を作り直すとき用）。</summary>
        public static string ToCSharpLines(LessonState state)
        {
            var builder = new StringBuilder();
            foreach (var line in LessonStateComparer.Describe(state))
            {
                builder.Append("            \"").Append(line.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\",\n");
            }

            return builder.ToString();
        }
    }
}
