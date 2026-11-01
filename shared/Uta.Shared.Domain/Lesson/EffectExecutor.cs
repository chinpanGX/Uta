using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Numerics;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>効果の処理（lesson-card.md 7章）。Op ごとの switch で切り替える。</summary>
    internal static class EffectExecutor
    {
        /// <summary>条件をまとめて判定してから、記載順に処理する（6.2）。</summary>
        public static void ExecuteAll(LessonState state, IReadOnlyList<EffectDef> effects, IRandom random)
        {
            Execute(state, effects, ConditionEvaluator.EvaluateEffects(state, effects), random);
        }

        /// <summary>判定済みの条件で、記載順に処理する。レッスンが終わったら、それ以降は処理しない。</summary>
        public static void Execute(LessonState state, IReadOnlyList<EffectDef> effects, bool[] passed, IRandom random)
        {
            for (var i = 0; i < effects.Count; i++)
            {
                if (state.IsFinished)
                {
                    return;
                }

                if (passed[i])
                {
                    ExecuteOne(state, effects[i], random);
                }
            }
        }

        private static void ExecuteOne(LessonState state, EffectDef effect, IRandom random)
        {
            switch (effect.Op)
            {
                case EffectOp.GainParam:
                {
                    var hits = Math.Max(1, effect.Arg1);
                    var focusScale = effect.Arg2 == 0 ? 100 : effect.Arg2;
                    var goodScale = effect.Arg3 == 0 ? 100 : effect.Arg3;
                    for (var hit = 0; hit < hits && !state.IsFinished; hit++)
                    {
                        // 連撃は1回ごとに計算して加算する（9.2）
                        GainParam(state, ComputeParamGain(state, effect.Value, focusScale, goodScale));
                    }

                    break;
                }
                case EffectOp.GainParamByImpression:
                    GainParamByRatio(state, state.Statuses.Get(StatusKind.Impression), effect.Value);
                    break;
                case EffectOp.GainParamByShield:
                    GainParamByRatio(state, state.Shield, effect.Value);
                    break;
                case EffectOp.GainParamByMotivation:
                    GainParamByRatio(state, state.Statuses.Get(StatusKind.Motivation), effect.Value);
                    break;
                case EffectOp.AddFixedShield:
                    state.Shield += LessonFormulas.FixedShieldGain(effect.Value, state.Statuses.Get(StatusKind.ShieldGainBlock) > 0);
                    break;
                case EffectOp.RecoverHp:
                    state.Hp = FixedMath.ToInt(Math.Min(state.MaxHp, (long)state.Hp + effect.Value));
                    break;
                case EffectOp.ScaleShield:
                    state.Shield = LessonFormulas.ScaledShield(state.Shield, effect.Value);
                    break;
                case EffectOp.SetShield:
                    state.Shield = effect.Value; // 元気増加無効でも設定する（lesson-card.md 7.2）
                    break;
                case EffectOp.AddShield:
                    state.Shield += LessonFormulas.ShieldGain(
                        effect.Value,
                        state.Statuses.Get(StatusKind.Motivation),
                        state.Statuses.Get(StatusKind.ShieldGainBlock) > 0);
                    break;
                case EffectOp.AddStatus:
                    state.Statuses.Add(effect.Status, effect.Value, state.Phase);
                    break;
                case EffectOp.DrawCards:
                    state.Zones.Draw(effect.Value, random);
                    break;
                case EffectOp.AddCardUses:
                    state.UsesLeft += effect.Value;
                    break;
                case EffectOp.AddPersistent:
                    state.AddPersistent(new PersistentEffect(
                        effect.Trigger,
                        effect.Children,
                        effect.Arg1 == 0 ? PersistentEffect.Unlimited : effect.Arg1,
                        effect.Arg2 == 0 ? PersistentEffect.Unlimited : effect.Arg2,
                        state.TakeGrantOrder(),
                        effect.Arg1 > 0 && state.Phase == LessonPhase.Player));
                    break;
                default:
                    // CardDefinitionValidator で読み込み時に弾く
                    throw new NotSupportedException("未対応の効果です: " + effect.Op);
            }
        }

        private static void GainParamByRatio(LessonState state, int source, int percent)
        {
            GainParam(state, ComputeParamGain(state, LessonFormulas.RatioParam(source, percent), 100, 100));
        }

        /// <summary>レッスンで得たパラメータを足す。パーフェクト目標に届いたら、その値で止めてレッスンを終える（4.2）。</summary>
        internal static void GainParam(LessonState state, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var perfect = state.Setup.PerfectTarget;
            var gained = state.ParamGained + amount;
            if (perfect > 0 && gained >= perfect)
            {
                state.ParamGained = perfect;
                state.Phase = LessonPhase.Finished;
                return;
            }

            state.ParamGained = gained;
        }

        internal static List<int> CollectBonusPercents(LessonState state)
        {
            var bonuses = new List<int>(state.ParamBonuses.Count);
            foreach (var entry in state.ParamBonuses)
            {
                bonuses.Add(entry.Percent);
            }

            return bonuses;
        }

        private static int ComputeParamGain(LessonState state, int cardParam, int focusScale, int goodScale)
        {
            var statuses = state.Statuses;
            return LessonFormulas.ParamGain(
                cardParam,
                statuses.Get(StatusKind.Focus),
                statuses.Get(StatusKind.FocusBoost),
                focusScale,
                statuses.Get(StatusKind.GoodCondition),
                statuses.Get(StatusKind.Excellent),
                goodScale,
                CollectBonusPercents(state),
                0);
        }
    }

    /// <summary>
    /// 同じタイミングの発動を順に処理する（lesson-card.md 10章）。
    /// 順番はサポートアビリティ → 育成アイテム → 持続効果（付与順）。いまは持続効果だけ。
    /// </summary>
    internal static class TriggerDispatcher
    {
        public static void Fire(LessonState state, IRandom random, PersistentTrigger trigger)
        {
            Fire(state, random, trigger, trigger);
        }

        /// <summary>2種類のトリガー（「使用時」と「アクティブ使用時」など）を、付与順でまとめて処理する。</summary>
        public static void Fire(LessonState state, IRandom random, PersistentTrigger first, PersistentTrigger second)
        {
            // 処理中に付与された持続効果は、このタイミングには反応しない
            var targets = new List<PersistentEffect>();
            foreach (var persistent in state.Persistents)
            {
                if (persistent.Trigger == first || persistent.Trigger == second)
                {
                    targets.Add(persistent);
                }
            }

            foreach (var persistent in targets)
            {
                if (state.IsFinished)
                {
                    break;
                }

                if (persistent.IsExpired)
                {
                    continue;
                }

                if (persistent.RemainingCount > 0)
                {
                    persistent.RemainingCount--;
                }

                EffectExecutor.ExecuteAll(state, persistent.Effects, random);
            }

            state.RemoveExpired();
        }
    }
}
