using System;
using System.Collections.Generic;

namespace Uta.Domain.Lesson
{
    /// <summary>条件の判定（lesson-card.md 6章）。</summary>
    public static class ConditionEvaluator
    {
        public static bool IsSatisfied(LessonState state, ConditionDef condition)
        {
            long left;
            long right = condition.Value;
            switch (condition.Subject)
            {
                case ConditionSubject.GoodConditionTurns:
                    left = state.Statuses.Get(StatusKind.GoodCondition);
                    break;
                case ConditionSubject.ExcellentTurns:
                    left = state.Statuses.Get(StatusKind.Excellent);
                    break;
                case ConditionSubject.Focus:
                    left = state.Statuses.Get(StatusKind.Focus);
                    break;
                case ConditionSubject.Impression:
                    left = state.Statuses.Get(StatusKind.Impression);
                    break;
                case ConditionSubject.Motivation:
                    left = state.Statuses.Get(StatusKind.Motivation);
                    break;
                case ConditionSubject.Genki:
                    left = state.Genki;
                    break;
                case ConditionSubject.HpPercent:
                    // 体力 / 最大体力 と 値% を、割り算せずに比べる
                    left = (long)state.Hp * 100;
                    right = (long)condition.Value * state.MaxHp;
                    break;
                case ConditionSubject.TurnNumber:
                    left = state.Turn;
                    break;
                case ConditionSubject.ClearTargetPercent:
                    left = (long)state.ParamGained * 100;
                    right = (long)condition.Value * state.Setup.ClearTarget;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(condition));
            }

            switch (condition.Comparator)
            {
                case Comparator.GreaterOrEqual:
                    return left >= right;
                case Comparator.LessOrEqual:
                    return left <= right;
                case Comparator.Equal:
                    return left == right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(condition));
            }
        }

        public static bool AreAllSatisfied(LessonState state, IReadOnlyList<ConditionDef> conditions)
        {
            for (var i = 0; i < conditions.Count; i++)
            {
                if (!IsSatisfied(state, conditions[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 同じタイミングで発動する効果の条件を、まとめて先に判定する（6.2）。結果は効果と同じ並び。
        /// </summary>
        public static bool[] EvaluateEffects(LessonState state, IReadOnlyList<EffectDef> effects)
        {
            var passed = new bool[effects.Count];
            for (var i = 0; i < effects.Count; i++)
            {
                passed[i] = AreAllSatisfied(state, effects[i].Conditions);
            }

            return passed;
        }
    }
}
