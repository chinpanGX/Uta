using System;
using System.Collections.Generic;
using Uta.Client.Application.Produce;
using Uta.Shared.Contracts.Produce;

namespace Uta.Client.Infrastructure.Produce
{
    /// <summary>DTO → Application の型。null の配列は空のリストにする。</summary>
    public static class ProduceMapper
    {
        public static ProduceStateResult ToStateResult(ProduceStateResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new ProduceStateResult(
                response.OperationNumber, response.Phase, response.ScenarioMasterId, response.IdolMasterId, response.Week,
                response.Singing, response.Rhythm, response.Expression, response.Hp, response.MaxHp, response.ExamRetryLeft,
                ToList(response.Deck, dto => new ProduceDeckCard(dto.InstanceId, dto.CardMasterId, dto.Enhanced)),
                ToList(response.WeekActions, dto => new ProduceWeekAction(dto.SlotIndex, dto.Kind, dto.Selectable)),
                ToList(response.ClassOptions, dto => new ProduceClassOption(dto.Index, dto.HpCost, dto.Param, dto.ParamGain, dto.GivesCard, dto.Selectable)),
                ToList(response.CardChoiceMasterIds),
                response.Lesson == null ? null : ToLesson(response.Lesson),
                response.ExamResult == null ? null : ToExamResult(response.ExamResult),
                response.Result == null ? null : ToEndResult(response.Result));
        }

        public static ProduceEndResult ToEndResult(ProduceResultResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new ProduceEndResult(
                (ProduceEndKind)(int)response.Ending, response.ExamResultKind, response.EvaluationValue, response.Rank, response.FinalScore,
                response.FinalRank, response.GachaCurrency, response.Gold, response.SupportPoints, response.Exp, response.PlayerExp);
        }

        private static ProduceLesson ToLesson(LessonDto dto)
        {
            return new ProduceLesson(
                dto.Kind, dto.TargetParam, dto.MaxTurns, dto.ClearTarget, dto.PerfectTarget,
                ToList(dto.ExamTurns, turn => new ProduceExamTurn(turn.Param, turn.ScoreBonusPercent)),
                dto.Turn, dto.Shield, dto.UsesLeft, dto.ParamGained,
                ToList(dto.Hand), ToList(dto.UsableHand), ToList(dto.DrawPile), ToList(dto.Discard), ToList(dto.Exiled),
                ToList(dto.StatusValues),
                ToList(dto.ParamBonuses, bonus => new ProduceParamBonus(bonus.Percent, bonus.RemainingTurns)),
                ToList(dto.Persistents, persistent => new ProducePersistent(persistent.Trigger, persistent.RemainingTurns, persistent.RemainingCount)),
                ToList(dto.LessonEnhanced), ToList(dto.NpcScores));
        }

        private static ProduceExamResult ToExamResult(ExamResultDto dto)
        {
            return new ProduceExamResult(dto.Kind, dto.Attempt, dto.Score, dto.Rank, ToList(dto.NpcScores), dto.Passed);
        }

        private static List<T> ToList<T>(T[] source)
        {
            return source == null ? new List<T>() : new List<T>(source);
        }

        private static List<TResult> ToList<TSource, TResult>(TSource[] source, Func<TSource, TResult> map)
        {
            var list = new List<TResult>();
            if (source == null)
            {
                return list;
            }

            foreach (var item in source)
            {
                list.Add(map(item));
            }

            return list;
        }
    }
}
