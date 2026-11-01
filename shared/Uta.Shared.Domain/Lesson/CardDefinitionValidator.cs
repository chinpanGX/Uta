using System.Collections.Generic;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>
    /// カード定義の検査。未対応の Op を使うカードは、マスタを読むときにエラーにする（lesson-card.md 15.1）。
    /// </summary>
    public static class CardDefinitionValidator
    {
        /// <summary>いまのエンジンが処理できる Op（フェーズ2まで。lesson-domain-types.md 6章）。</summary>
        public static bool IsSupported(EffectOp op)
        {
            switch (op)
            {
                case EffectOp.GainParam:
                case EffectOp.GainParamByImpression:
                case EffectOp.GainParamByShield:
                case EffectOp.GainParamByMotivation:
                case EffectOp.AddFixedShield:
                case EffectOp.RecoverHp:
                case EffectOp.ScaleShield:
                case EffectOp.SetShield:
                case EffectOp.AddShield:
                case EffectOp.AddStatus:
                case EffectOp.DrawCards:
                case EffectOp.AddCardUses:
                case EffectOp.AddPersistent:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>問題の一覧を返す。空なら問題なし。</summary>
        public static IReadOnlyList<string> Validate(CardDefinition card)
        {
            var errors = new List<string>();
            if (card.Plan == Plan.Nothing || card.Type == CardType.Nothing || card.Rarity == Rarity.Nothing)
            {
                errors.Add("カード " + card.Id + ": カードの区分が不正");
            }

            ValidateFace(card, card.Base, "通常版", errors);
            if (card.Enhanced != null)
            {
                ValidateFace(card, card.Enhanced, "強化版", errors);
            }

            return errors;
        }

        private static void ValidateFace(CardDefinition card, CardFace face, string faceName, List<string> errors)
        {
            var prefix = "カード " + card.Id + " の" + faceName + ": ";
            foreach (var cost in face.Costs)
            {
                if (!IsDefined(cost.Kind))
                {
                    errors.Add(prefix + "コストの種類が不正");
                }
            }

            foreach (var condition in face.UseConditions)
            {
                ValidateCondition(condition, prefix, errors);
            }

            foreach (var effect in face.Effects)
            {
                ValidateEffect(effect, prefix, errors, false);
            }
        }

        private static void ValidateCondition(ConditionDef condition, string prefix, List<string> errors)
        {
            if (!IsDefined(condition.Subject) || !IsDefined(condition.Comparator))
            {
                errors.Add(prefix + "条件が不正");
            }
        }

        private static void ValidateEffect(EffectDef effect, string prefix, List<string> errors, bool insidePersistent)
        {
            if (!IsSupported(effect.Op))
            {
                errors.Add(prefix + "未対応の効果 " + effect.Op);
                return;
            }

            foreach (var condition in effect.Conditions)
            {
                ValidateCondition(condition, prefix, errors);
            }

            if (effect.Value < 0 || effect.Arg1 < 0 || effect.Arg2 < 0 || effect.Arg3 < 0)
            {
                errors.Add(prefix + effect.Op + " の値が負");
            }

            if ((effect.Op == EffectOp.AddStatus || effect.Op == EffectOp.DrawCards) && effect.Value < 1)
            {
                errors.Add(prefix + effect.Op + " の値が 1 未満");
            }

            if ((effect.Op == EffectOp.GainParamByShield || effect.Op == EffectOp.GainParamByMotivation
                    || effect.Op == EffectOp.AddFixedShield || effect.Op == EffectOp.RecoverHp) && effect.Value < 1)
            {
                errors.Add(prefix + effect.Op + " の値が 1 未満");
            }

            if (effect.Op == EffectOp.ScaleShield && effect.Value > 100)
            {
                errors.Add(prefix + effect.Op + " の値が 100 を超える");
            }

            if (effect.Op == EffectOp.AddStatus && !IsAddableStatus(effect.Status))
            {
                errors.Add(prefix + effect.Op + " のステータスが不正");
            }

            if (effect.Op == EffectOp.AddPersistent && !IsPersistentTrigger(effect.Trigger))
            {
                errors.Add(prefix + effect.Op + " のトリガーが不正");
            }

            if ((effect.Op != EffectOp.AddStatus && effect.Status != StatusKind.Nothing)
                || (effect.Op != EffectOp.AddPersistent && effect.Trigger != PersistentTrigger.Nothing))
            {
                errors.Add(prefix + effect.Op + " に使わない列がある");
            }

            if (effect.Op == EffectOp.AddPersistent)
            {
                if (effect.Children.Count == 0)
                {
                    errors.Add(prefix + "持続効果に発動する効果が無い");
                }

                if (insidePersistent)
                {
                    errors.Add(prefix + "持続効果の中に持続効果がある");
                }

                if (effect.Trigger == PersistentTrigger.LessonStart)
                {
                    foreach (var child in effect.Children)
                    {
                        if (child.Op == EffectOp.DrawCards || child.Op == EffectOp.ReplaceHand
                            || child.Op == EffectOp.EnhanceHand || child.Op == EffectOp.CreateCard)
                        {
                            errors.Add(prefix + "レッスン開始時の効果が " + child.Op + " でゾーンを操作している");
                        }
                    }
                }

                foreach (var child in effect.Children)
                {
                    ValidateEffect(child, prefix, errors, true);
                }
            }
        }

        // Enum.IsDefined はリフレクションを使うため、範囲で確かめる
        private static bool IsAddableStatus(StatusKind value)
        {
            return value >= StatusKind.GoodCondition
                && value <= StatusKind.DebuffGuard
                && value != StatusKind.FocusBoost
                && value != StatusKind.ImpressionBoost;
        }

        private static bool IsPersistentTrigger(PersistentTrigger value)
        {
            return value >= PersistentTrigger.LessonStart
                && value <= PersistentTrigger.TurnEnd
                && value != PersistentTrigger.EveryNTurns;
        }

        private static bool IsDefined(CostKind value)
        {
            return value >= CostKind.Consume && value <= CostKind.Motivation;
        }

        private static bool IsDefined(ConditionSubject value)
        {
            return value >= ConditionSubject.GoodConditionTurns && value <= ConditionSubject.ClearTargetPercent;
        }

        private static bool IsDefined(Comparator value)
        {
            return value >= Comparator.GreaterOrEqual && value <= Comparator.Equal;
        }
    }
}
