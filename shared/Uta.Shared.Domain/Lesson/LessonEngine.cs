using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>
    /// レッスンの処理（lesson-card.md 10章）。1回の呼び出しで、次の入力待ち（プレイヤー操作）まで進める（produce.md 10.1）。
    /// 乱数は Lesson ストリーム（produce.md 10.2）を渡す。
    /// </summary>
    public sealed class LessonEngine
    {
        private readonly ICardCatalog catalog;
        private readonly LessonRules rules;

        public LessonEngine(ICardCatalog catalog, LessonRules rules)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>レッスンを始め、1ターン目の入力待ちまで進める（L1・L2、T1〜T7）。</summary>
        public void Start(LessonState state, IRandom random)
        {
            if (state.Phase != LessonPhase.NotStarted)
            {
                throw new InvalidOperationException("レッスンはすでに始まっています。");
            }

            if (state.Setup.IsExam)
            {
                // 試験（審査ターン・スコア）は後のフェーズで実装する
                throw new NotSupportedException("試験はまだ対応していません。");
            }

            state.Phase = LessonPhase.LessonStart;

            // L1. 「レッスン開始時」効果
            TriggerDispatcher.Fire(state, random, PersistentTrigger.LessonStart);

            // L2. 山札のシャッフルと、開始時手札カードの配置（produce.md 10.3）
            PlaceInitialCards(state, random);

            BeginTurn(state, random);
        }

        /// <summary>カードを使えるか。乱数を使わず、状態も変えない。</summary>
        public UseCheckResult CanUse(LessonState state, int instanceId)
        {
            if (state.Phase != LessonPhase.Player)
            {
                return UseCheckResult.NotPlayerPhase;
            }

            if (!Contains(state.Zones.Hand, instanceId))
            {
                return UseCheckResult.NotInHand;
            }

            if (state.UsesLeft <= 0)
            {
                return UseCheckResult.NoUsesLeft;
            }

            var face = GetFace(state, instanceId);
            if (!ConditionEvaluator.AreAllSatisfied(state, face.UseConditions))
            {
                return UseCheckResult.UseConditionNotMet;
            }

            return CanPay(state, face.Costs) ? UseCheckResult.Ok : UseCheckResult.CannotPayCost;
        }

        /// <summary>
        /// プレイヤー操作を処理し、次の入力待ち（またはレッスンの終了）まで進める。
        /// Accepted 以外を返したときは、状態を変えていない。
        /// </summary>
        public LessonApplyResult Apply(LessonState state, LessonCommand command, IRandom random)
        {
            if (state.Phase != LessonPhase.Player)
            {
                return LessonApplyResult.NotPlayerPhase;
            }

            switch (command.Kind)
            {
                case LessonCommandKind.UseCard:
                    return UseCard(state, command.Arg, random);
                case LessonCommandKind.Skip:
                    // スキップの体力回復は E1 より前に行う（lesson-domain-types.md 5章）
                    state.Hp = Math.Min(state.MaxHp, state.Hp + rules.SkipHpRecover);
                    EndTurn(state, random, true);
                    return LessonApplyResult.Accepted;
                case LessonCommandKind.UseDrink:
                    // ドリンクはフェーズ5
                    return LessonApplyResult.NotSupported;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command));
            }
        }

        private LessonApplyResult UseCard(LessonState state, int instanceId, IRandom random)
        {
            // P1. 使用条件（とコストを払えるか）の判定
            var check = CanUse(state, instanceId);
            if (check != UseCheckResult.Ok)
            {
                return (LessonApplyResult)check;
            }

            var definition = catalog.Get(state.GetCard(instanceId).CardId);
            var face = definition.GetFace(state.IsTreatedAsEnhanced(instanceId));

            // P2. 効果内条件の判定（コストを払う前）
            var passed = ConditionEvaluator.EvaluateEffects(state, face.Effects);

            // P3. コストを払う
            var hpBefore = state.Hp;
            Pay(state, face.Costs);
            state.UsesLeft--;
            state.Zones.RemoveFromHand(instanceId);

            // P4. 「コストで体力減少時」
            if (state.Hp < hpBefore)
            {
                TriggerDispatcher.Fire(state, random, PersistentTrigger.OnCostHpDecrease);
            }

            // P5. 「スキルカード使用時」
            FireCardTrigger(state, random, definition.Type, PersistentTrigger.OnCardUse, PersistentTrigger.OnActiveCardUse, PersistentTrigger.OnMentalCardUse);

            // P6. カード効果を上から1行ずつ
            EffectExecutor.Execute(state, face.Effects, passed, random);

            // 使ったカードは効果の処理の後に移す（lesson-domain-types.md 5章）
            if (definition.OnceInLesson)
            {
                state.Zones.AddToExiled(instanceId);
            }
            else
            {
                state.Zones.AddToDiscard(instanceId);
            }

            // P7. 「スキルカード使用後」
            FireCardTrigger(state, random, definition.Type, PersistentTrigger.AfterCardUse, PersistentTrigger.AfterActiveCardUse, PersistentTrigger.AfterMentalCardUse);

            // P8. 使用数が残っていればプレイヤー操作に戻る
            if (!state.IsFinished && state.UsesLeft <= 0)
            {
                EndTurn(state, random, false);
            }

            return LessonApplyResult.Accepted;
        }

        private void BeginTurn(LessonState state, IRandom random)
        {
            state.Turn++;
            state.Phase = LessonPhase.TurnStart;
            state.UsesLeft = rules.BaseUses;

            // T1. 発動予約の残りターンを1減らす
            foreach (var scheduled in state.Scheduled)
            {
                scheduled.RemainingTurns--;
            }

            // T2. 好印象を1減らす
            state.Statuses.Add(StatusKind.Impression, -1, state.Phase);

            // T3. 「ターン開始時」
            TriggerDispatcher.Fire(state, random, PersistentTrigger.TurnStart);
            if (state.IsFinished)
            {
                return;
            }

            // T4. ドロー。1ターン目は開始時手札を含めて最低枚数まで
            var drawCount = state.Turn == 1
                ? Math.Max(0, rules.FirstTurnMinHand - state.Zones.Hand.Count)
                : rules.DrawPerTurn;
            state.Zones.Draw(drawCount, random);

            // T5. 「ターン開始後」
            TriggerDispatcher.Fire(state, random, PersistentTrigger.AfterTurnStart);

            // T6. 残り0になった発動予約（付与順）
            var due = new List<ScheduledEffect>();
            foreach (var scheduled in state.Scheduled)
            {
                if (scheduled.RemainingTurns <= 0)
                {
                    due.Add(scheduled);
                }
            }

            foreach (var scheduled in due)
            {
                state.RemoveScheduled(scheduled);
                EffectExecutor.ExecuteAll(state, scheduled.Effects, random);
            }

            // T7. 「Nターンごと」は、使うカード・アイテムを採用するときに実装する

            if (!state.IsFinished)
            {
                state.Phase = LessonPhase.Player;
            }
        }

        private void EndTurn(LessonState state, IRandom random, bool skipped)
        {
            state.Phase = LessonPhase.TurnEnd;

            // E1. 「ターンスキップ時」
            if (skipped)
            {
                TriggerDispatcher.Fire(state, random, PersistentTrigger.OnSkip);
            }

            // E2. 「ターン終了時」
            TriggerDispatcher.Fire(state, random, PersistentTrigger.TurnEnd);
            if (state.IsFinished)
            {
                return;
            }

            // E3. 好印象によるパラメータ上昇
            var impression = state.Statuses.Get(StatusKind.Impression);
            if (impression > 0)
            {
                var gain = LessonFormulas.ImpressionGain(
                    impression,
                    state.Statuses.Get(StatusKind.ImpressionBoost),
                    EffectExecutor.CollectBonusPercents(state));
                EffectExecutor.GainParam(state, gain);
                if (state.IsFinished)
                {
                    return;
                }
            }

            // E4. 手札を捨札へ
            state.Zones.DiscardHand();

            // E5. ターン制ステータスの残りターンを1減らす（発動予約以外）
            state.Statuses.DecrementTurns();
            foreach (var bonus in state.ParamBonuses)
            {
                DecrementTurns(bonus);
            }

            foreach (var persistent in state.Persistents)
            {
                DecrementTurns(persistent);
            }

            state.RemoveExpired();

            // E6. 残りターンがあれば次のターンへ
            if (state.Turn >= state.Setup.MaxTurns)
            {
                state.Phase = LessonPhase.Finished;
                return;
            }

            BeginTurn(state, random);
        }

        private void PlaceInitialCards(LessonState state, IRandom random)
        {
            var startCards = new List<int>();
            var others = new List<int>();
            foreach (var card in SortedByInstanceId(state.Deck))
            {
                if (catalog.Get(card.CardId).StartInHand)
                {
                    startCards.Add(card.InstanceId);
                }
                else
                {
                    others.Add(card.InstanceId);
                }
            }

            var hand = startCards;
            if (startCards.Count > rules.StartInHandMax)
            {
                // 上限を超えたら、開始時手札の組をシャッフルして先頭から手札へ。残りは山札に混ぜる
                RandomOps.Shuffle(random, startCards);
                hand = startCards.GetRange(0, rules.StartInHandMax);
                others.AddRange(startCards.GetRange(rules.StartInHandMax, startCards.Count - rules.StartInHandMax));
                others.Sort();
            }

            RandomOps.Shuffle(random, others);
            state.Zones.SetUp(hand, others);
        }

        private CardFace GetFace(LessonState state, int instanceId)
        {
            var definition = catalog.Get(state.GetCard(instanceId).CardId);
            return definition.GetFace(state.IsTreatedAsEnhanced(instanceId));
        }

        private static void FireCardTrigger(
            LessonState state,
            IRandom random,
            CardType type,
            PersistentTrigger any,
            PersistentTrigger active,
            PersistentTrigger mental)
        {
            if (state.IsFinished)
            {
                return;
            }

            switch (type)
            {
                case CardType.Active:
                    TriggerDispatcher.Fire(state, random, any, active);
                    break;
                case CardType.Mental:
                    TriggerDispatcher.Fire(state, random, any, mental);
                    break;
                default:
                    TriggerDispatcher.Fire(state, random, any);
                    break;
            }
        }

        private static bool CanPay(LessonState state, IReadOnlyList<CostDef> costs)
        {
            foreach (var cost in costs)
            {
                if (cost.Value == 0)
                {
                    continue;
                }

                switch (cost.Kind)
                {
                    case CostKind.Consume:
                        if ((long)state.Genki + state.Hp < FinalConsume(state, cost.Value))
                        {
                            return false;
                        }

                        break;
                    case CostKind.HpDirect:
                        if (state.Hp < cost.Value)
                        {
                            return false;
                        }

                        break;
                    default:
                        if (state.Statuses.Get(ToStatus(cost.Kind)) < cost.Value)
                        {
                            return false;
                        }

                        break;
                }
            }

            return true;
        }

        private static void Pay(LessonState state, IReadOnlyList<CostDef> costs)
        {
            foreach (var cost in costs)
            {
                switch (cost.Kind)
                {
                    case CostKind.Consume:
                    {
                        // 元気から先に払い、足りない分を体力から
                        var amount = FinalConsume(state, cost.Value);
                        var fromGenki = Math.Min(state.Genki, amount);
                        state.Genki -= fromGenki;
                        state.Hp -= amount - fromGenki;
                        break;
                    }
                    case CostKind.HpDirect:
                        state.Hp -= cost.Value;
                        break;
                    default:
                        state.Statuses.Add(ToStatus(cost.Kind), -cost.Value, state.Phase);
                        break;
                }
            }
        }

        private static int FinalConsume(LessonState state, int cost)
        {
            var statuses = state.Statuses;
            return LessonFormulas.FinalConsume(
                cost,
                statuses.Get(StatusKind.CostHalf) > 0,
                statuses.Get(StatusKind.CostDouble) > 0,
                0,
                statuses.Get(StatusKind.CostReduce));
        }

        private static StatusKind ToStatus(CostKind kind)
        {
            switch (kind)
            {
                case CostKind.GoodConditionTurns:
                    return StatusKind.GoodCondition;
                case CostKind.ExcellentTurns:
                    return StatusKind.Excellent;
                case CostKind.Focus:
                    return StatusKind.Focus;
                case CostKind.Impression:
                    return StatusKind.Impression;
                case CostKind.Motivation:
                    return StatusKind.Motivation;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static void DecrementTurns(ParamBonusEntry entry)
        {
            if (entry.SkipNextDecrement)
            {
                entry.SkipNextDecrement = false;
            }
            else if (entry.RemainingTurns > 0)
            {
                entry.RemainingTurns--;
            }
        }

        private static void DecrementTurns(PersistentEffect persistent)
        {
            if (persistent.RemainingTurns == PersistentEffect.Unlimited)
            {
                return;
            }

            if (persistent.SkipNextDecrement)
            {
                persistent.SkipNextDecrement = false;
            }
            else if (persistent.RemainingTurns > 0)
            {
                persistent.RemainingTurns--;
            }
        }

        private static List<CardInstance> SortedByInstanceId(IReadOnlyList<CardInstance> deck)
        {
            var sorted = new List<CardInstance>(deck);
            sorted.Sort((a, b) => a.InstanceId.CompareTo(b.InstanceId));
            return sorted;
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
