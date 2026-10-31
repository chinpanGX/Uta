using System.Collections.Generic;

namespace Uta.Shared.Domain.Lesson
{
    /// <summary>
    /// カード定義の検査。未対応の Op を使うカードは、マスタを読むときにエラーにする（lesson-card.md 15.1）。
    /// </summary>
    public static class CardDefinitionValidator
    {
        /// <summary>いまのエンジンが処理できる Op（フェーズ1。lesson-domain-types.md 6章）。</summary>
        public static bool IsSupported(EffectOp op)
        {
            switch (op)
            {
                case EffectOp.Param:
                case EffectOp.ParamByRatio:
                case EffectOp.AddGenki:
                case EffectOp.AddTurns:
                case EffectOp.AddStack:
                case EffectOp.Draw:
                case EffectOp.AddUses:
                case EffectOp.Persistent:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>問題の一覧を返す。空なら問題なし。</summary>
        public static IReadOnlyList<string> Validate(CardDefinition card)
        {
            var errors = new List<string>();
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
            foreach (var effect in face.Effects)
            {
                ValidateEffect(effect, prefix, errors, false);
            }
        }

        private static void ValidateEffect(EffectDef effect, string prefix, List<string> errors, bool insidePersistent)
        {
            if (!IsSupported(effect.Op))
            {
                errors.Add(prefix + "未対応の効果 " + effect.Op);
                return;
            }

            switch (effect.Op)
            {
                case EffectOp.Param:
                    if (effect.Value < 0 || effect.Arg1 < 0 || effect.Arg2 < 0 || effect.Arg3 < 0)
                    {
                        errors.Add(prefix + "Param の値が負");
                    }

                    break;
                case EffectOp.ParamByRatio:
                    if (effect.Value < 0 || !IsDefined((RatioSource)effect.Arg1))
                    {
                        errors.Add(prefix + "ParamByRatio の引数が不正");
                    }

                    break;
                case EffectOp.AddTurns:
                case EffectOp.AddStack:
                    if (!IsDefined((StatusKind)effect.Arg1))
                    {
                        errors.Add(prefix + effect.Op + " のステータスが不正");
                    }

                    break;
                case EffectOp.Draw:
                case EffectOp.AddUses:
                case EffectOp.AddGenki:
                    if (effect.Value < 0)
                    {
                        errors.Add(prefix + effect.Op + " の値が負");
                    }

                    break;
                case EffectOp.Persistent:
                    if (insidePersistent)
                    {
                        errors.Add(prefix + "持続効果の中に持続効果がある");
                    }

                    if (!IsDefined((PersistentTrigger)effect.Arg1) || effect.Arg2 < 0 || effect.Arg3 < 0)
                    {
                        errors.Add(prefix + "Persistent の引数が不正");
                    }

                    if (effect.Children.Count == 0)
                    {
                        errors.Add(prefix + "Persistent に発動する効果が無い");
                    }

                    foreach (var child in effect.Children)
                    {
                        ValidateEffect(child, prefix, errors, true);
                    }

                    break;
            }
        }

        // Enum.IsDefined はリフレクションを使うため、範囲で確かめる
        private static bool IsDefined(RatioSource value)
        {
            return value >= RatioSource.Impression && value <= RatioSource.Motivation;
        }

        private static bool IsDefined(StatusKind value)
        {
            return value >= StatusKind.GoodCondition && value <= StatusKind.DebuffGuard;
        }

        private static bool IsDefined(PersistentTrigger value)
        {
            return value >= PersistentTrigger.LessonStart && value <= PersistentTrigger.TurnEnd;
        }
    }
}
