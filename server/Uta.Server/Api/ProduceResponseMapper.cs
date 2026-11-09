using Uta.Server.Application.Produce;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Api;

/// <summary>ProduceStateResult / ProduceEndResult を Contracts のレスポンスに変える。</summary>
public static class ProduceResponseMapper
{
    public static ProduceStateResponse ToStateResponse(ProduceStateResult result)
    {
        var state = result.State;
        if (state is null)
        {
            return new ProduceStateResponse(
                0, ProducePhase.Nothing, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                [], [], [], [], null!, null!, null!);
        }

        var exam = result.ExamResult;
        return new ProduceStateResponse(
            state.NextOperationNumber,
            state.Phase,
            state.Input.ScenarioId,
            state.Input.IdolId,
            state.Week,
            state.GetParam(ParamKind.Singing),
            state.GetParam(ParamKind.Rhythm),
            state.GetParam(ParamKind.Expression),
            state.Lesson?.Hp ?? state.Hp,
            state.MaxHp,
            state.ExamRetryLeft,
            state.Deck.OrderBy(x => x.InstanceId).Select(x => new DeckCardDto(x.InstanceId, x.CardId, x.Enhanced)).ToArray(),
            result.WeekChoices.Select(x => new WeekActionDto(x.SlotIndex, x.Kind, x.Selectable)).ToArray(),
            result.ClassOptions.Select(x => new ClassOptionDto(x.Index, x.HpCost, x.Param, x.ParamGain, x.GivesCard, x.Selectable)).ToArray(),
            state.OfferCards.ToArray(),
            state.Lesson is null ? null! : ToLessonDto(state.Lesson, result.NpcScores),
            exam is null ? null! : new ExamResultDto(exam.Kind, exam.Attempt, exam.Score, exam.Rank, exam.NpcScores.ToArray(), exam.Passed),
            result.End is null ? null! : ToResultResponse(result.End));
    }

    public static ProduceResultResponse ToResultResponse(ProduceEndResult end) => new(
        (ProduceEnding)(int)end.Ending, end.ExamOutcome, end.EvaluationValue, end.Rank, end.FinalScore, end.FinalRank,
        end.GachaCurrency, end.Gold, end.SupportPoints, end.Exp, end.PlayerLevel, end.PlayerExp);

    private static LessonDto ToLessonDto(LessonState lesson, IReadOnlyList<int> npcScores)
    {
        var setup = lesson.Setup;
        var examTurns = setup.Exam is null
            ? []
            : setup.Exam.Turns.Select(x => new ExamTurnDto(x.Param, x.ScoreBonusPercent)).ToArray();
        var statusValues = Enumerable.Range(0, StatusSet.KindCount).Select(i => lesson.Statuses.Get((StatusKind)i)).ToArray();
        return new LessonDto(
            setup.Kind,
            setup.TargetParam,
            setup.MaxTurns,
            setup.ClearTarget,
            setup.PerfectTarget,
            examTurns,
            lesson.Turn,
            lesson.Shield,
            lesson.UsesLeft,
            lesson.ParamGained,
            lesson.Zones.Hand.ToArray(),
            lesson.Zones.DrawPile.Order().ToArray(),
            lesson.Zones.Discard.Order().ToArray(),
            lesson.Zones.Exiled.Order().ToArray(),
            statusValues,
            lesson.ParamBonuses.Select(x => new ParamBonusDto(x.Percent, x.RemainingTurns)).ToArray(),
            lesson.Persistents.Select(x => new PersistentDto(x.Trigger, x.RemainingTurns, x.RemainingCount)).ToArray(),
            lesson.LessonEnhanced.ToArray(),
            npcScores.ToArray());
    }
}
