using System;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// IL2CPP の計算の一致を確かめる再生（lesson-phases.md 8章、build-ci.md 5.2）。
    /// フェーズ 1〜4 の 36 枚を 1 枚ずつ入れたデッキで、使えるカードがあれば手札の先頭から使い、なければスキップする。
    /// 値は LessonReplayTests の Replay_SameSeedAndCommands_SameResult（通常）・Replay_Exam_SameSeedAndCommands_SameResult（試験）と同じ。
    /// </summary>
    public static class LessonReplayScenario
    {
        public const int NormalHp = 30;
        public const ulong NormalSeed = 1;
        public const int ExamHp = 40;
        public const ulong ExamSeed = 2;

        /// <summary>通常のレッスン（リズム・7 ターン・クリア 1000・パーフェクト 2000）。HP 30、シード 1、attempt 0。</summary>
        public static LessonState RunNormal()
        {
            return Run(new LessonSetup(LessonKind.Normal, ParamKind.Rhythm, 7, 1000, 2000), NormalHp, NormalSeed);
        }

        /// <summary>中間試験（7 ターン。ボーナス 100・120・150・200・100・150・200%、歌・リズム・表現の順に繰り返す。早期終了 300）。HP 40、シード 2、attempt 0。</summary>
        public static LessonState RunExam()
        {
            var percents = new[] { 100, 120, 150, 200, 100, 150, 200 };
            var kinds = new[] { ParamKind.Singing, ParamKind.Rhythm, ParamKind.Expression };
            var turns = percents.Select((p, i) => new ExamTurn(kinds[i % 3], p)).ToArray();
            var setup = new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 7, 0, 0, new ExamSetup(turns, 300));
            return Run(setup, ExamHp, ExamSeed);
        }

        private static LessonState Run(LessonSetup setup, int hp, ulong seed)
        {
            var engine = new LessonEngine(new TestCardCatalog());
            var cardIds = Phase1Cards.AllIds
                .Concat(Phase2Cards.AllIds)
                .Concat(Phase3Cards.AllIds)
                .Concat(Phase4Cards.AllIds)
                .ToArray();
            var deck = cardIds.Select((cardId, index) => new CardInstance(index + 1, cardId, false)).ToList();
            var state = new LessonState(setup, deck, hp, hp);
            var random = RandomStreams.Create(seed, new RandomStreamKey(RandomStreamKind.Lesson, 3, 0));

            engine.Start(state, random);
            while (!state.IsFinished)
            {
                var usable = state.Zones.Hand.FirstOrDefault(id => engine.CanUse(state, id) == UseCheckResultKind.Ok);
                var command = usable != 0 ? LessonCommand.UseCard(usable) : LessonCommand.Skip();
                var result = engine.Apply(state, command, random);
                if (result != LessonApplyResultKind.Accepted)
                {
                    throw new InvalidOperationException("再生の操作が受け付けられませんでした: " + result);
                }
            }

            return state;
        }
    }
}
