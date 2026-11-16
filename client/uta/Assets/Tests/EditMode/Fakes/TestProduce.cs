using System.Collections.Generic;
using System.Linq;
using Uta.Client.Application.Produce;
using Uta.Client.Domain.Player;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    /// <summary>プロデュースのテスト用の組み立て。</summary>
    public static class TestProduce
    {
        /// <summary>scenarioId = 1、idolId = 1、examRetryLeft = 1。null の配列は空のリスト。</summary>
        public static ProduceStateResult StateResult(ProducePhase phase = ProducePhase.WeekSelect, int operationNumber = 0, int week = 1,
            int singing = 100, int rhythm = 100, int expression = 100, int hp = 30, int maxHp = 30,
            IReadOnlyList<ProduceDeckCard> deck = null, IReadOnlyList<ProduceWeekAction> weekActions = null,
            IReadOnlyList<ProduceClassOption> classOptions = null, IReadOnlyList<int> cardOffers = null,
            ProduceLesson lesson = null, ProduceExamResult examResult = null, ProduceEndResult result = null)
        {
            return new ProduceStateResult(operationNumber, phase, 1, 1, week, singing, rhythm, expression, hp, maxHp, 1,
                deck ?? new List<ProduceDeckCard>(), weekActions ?? new List<ProduceWeekAction>(),
                classOptions ?? new List<ProduceClassOption>(), cardOffers ?? new List<int>(), lesson, examResult, result);
        }

        /// <summary>ProduceEndKind.Abandoned、ほかは 0（playerExp = 0）。</summary>
        public static ProduceEndResult Abandoned()
        {
            return new ProduceEndResult(ProduceEndKind.Abandoned, ExamResultKind.Nothing, 0, ProduceRank.Nothing, 0, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>uniqueId = "P1"、exp・gold・supportPoints = 0、discardedProduce = false。</summary>
        public static PlayerEntity Player(int gachaCurrency = 0, bool hasActiveProduce = false)
        {
            return new PlayerEntity("P1", 0, 0, 0, gachaCurrency, hasActiveProduce, false);
        }
    }
}
