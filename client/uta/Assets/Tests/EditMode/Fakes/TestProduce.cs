using System.Collections.Generic;
using System.Linq;
using Uta.Client.Application.Produce;
using Uta.Client.Domain.Player;
using Uta.Shared.Contracts.Produce;
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

        /// <summary>ProduceStateResponse（応答の DTO）。null の配列は空の配列。</summary>
        public static ProduceStateResponse StateResponse(int operationNumber = 0, ProducePhase phase = ProducePhase.WeekSelect, int scenarioId = 1, int idolId = 1,
            int week = 1, int singing = 100, int rhythm = 100, int expression = 100, int hp = 30, int maxHp = 30, int examRetryLeft = 1,
            DeckCardDto[] deck = null, WeekActionDto[] weekActions = null, ClassOptionDto[] classOptions = null, int[] cardOffers = null,
            LessonDto lesson = null, ExamResultDto examResult = null, ProduceResultResponse result = null)
        {
            return new ProduceStateResponse(operationNumber, phase, scenarioId, idolId, week, singing, rhythm, expression, hp, maxHp, examRetryLeft,
                deck ?? new DeckCardDto[0], weekActions ?? new WeekActionDto[0], classOptions ?? new ClassOptionDto[0], cardOffers ?? new int[0],
                lesson, examResult, result);
        }

        /// <summary>ProduceResultResponse（放棄の既定。ほかの引数は 0・ExamResultKind.Nothing・ProduceRank.Nothing）。</summary>
        public static ProduceResultResponse ResultResponse(ProduceEnding ending = ProduceEnding.Abandoned)
        {
            return new ProduceResultResponse(ending, ExamResultKind.Nothing, 0, ProduceRank.Nothing, 0, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>uniqueId = "P1"、exp・gold・supportPoints = 0、discardedProduce = false。</summary>
        public static PlayerEntity Player(int gachaCurrency = 0, bool hasActiveProduce = false)
        {
            return new PlayerEntity("P1", 0, 0, 0, gachaCurrency, hasActiveProduce, false);
        }
    }
}
