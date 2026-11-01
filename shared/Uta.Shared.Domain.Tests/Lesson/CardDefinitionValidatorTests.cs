using System;
using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class CardDefinitionValidatorTests
    {
        private static CardDefinition CardWith(params EffectDef[] effects)
        {
            var face = new CardFace(new CostDef[0], new ConditionDef[0], effects);
            return new CardDefinition(999, Plan.Logic, CardType.Active, Rarity.SR, 1, true, false, false, face, null);
        }

        [Fact]
        public void Validate_OpNothing_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.Nothing));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("未対応の効果", errors[0]);
        }

        [Fact]
        public void Validate_AddStatusWithPercentStatus_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.AddStatus, status: StatusKind.FocusBoost, value: 1));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("のステータスが不正", errors[0]);
        }

        [Fact]
        public void Validate_AddStatusValueZero_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.AddStatus, status: StatusKind.Impression, value: 0));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("の値が 1 未満", errors[0]);
        }

        [Fact]
        public void Validate_AddStatusWithTrigger_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.AddStatus, status: StatusKind.Impression, trigger: PersistentTrigger.TurnStart, value: 1));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("に使わない列がある", errors[0]);
        }

        [Fact]
        public void Validate_AddPersistentWithEveryNTurns_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.EveryNTurns, children: new[] { new EffectDef(EffectOp.GainParam, value: 1) }));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("のトリガーが不正", errors[0]);
        }

        [Fact]
        public void Validate_AddPersistentWithoutChildren_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.TurnStart));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("持続効果に発動する効果が無い", errors[0]);
        }

        [Fact]
        public void Validate_NestedPersistent_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.TurnStart, children: new[] { new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.TurnStart, children: new[] { new EffectDef(EffectOp.GainParam, value: 1) }) }));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("持続効果の中に持続効果がある", errors[0]);
        }

        [Fact]
        public void Validate_AddParamBonus_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.AddParamBonus, value: 50));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("未対応の効果", errors[0]);
        }

        [Theory]
        [InlineData(EffectOp.GainParamByShield)]
        [InlineData(EffectOp.GainParamByMotivation)]
        [InlineData(EffectOp.AddFixedShield)]
        [InlineData(EffectOp.RecoverHp)]
        public void Validate_Phase2OpValueZero_ReturnsError(EffectOp op)
        {
            var errors = CardDefinitionValidator.Validate(CardWith(new EffectDef(op, value: 0)));
            Assert.Single(errors);
            Assert.Contains("の値が 1 未満", errors[0]);
        }

        [Fact]
        public void Validate_ScaleShield100_NoErrors()
        {
            Assert.Empty(CardDefinitionValidator.Validate(CardWith(new EffectDef(EffectOp.ScaleShield, value: 100))));
        }

        [Fact]
        public void Validate_ScaleShield101_ReturnsError()
        {
            var errors = CardDefinitionValidator.Validate(CardWith(new EffectDef(EffectOp.ScaleShield, value: 101)));
            Assert.Single(errors);
            Assert.Contains("の値が 100 を超える", errors[0]);
        }

        [Fact]
        public void Validate_SetShieldZero_NoErrors()
        {
            Assert.Empty(CardDefinitionValidator.Validate(CardWith(new EffectDef(EffectOp.SetShield, value: 0))));
        }

        [Fact]
        public void Validate_SetShieldNegative_ReturnsError()
        {
            var errors = CardDefinitionValidator.Validate(CardWith(new EffectDef(EffectOp.SetShield, value: -1)));
            Assert.Single(errors);
            Assert.Contains("の値が負", errors[0]);
        }

        [Fact]
        public void Validate_LessonStartPersistentWithDrawCards_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.LessonStart, children: new[] { new EffectDef(EffectOp.DrawCards, value: 1) }));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("ゾーンを操作している", errors[0]);
        }

        [Fact]
        public void Validate_LessonStartPersistentWithAddStatus_NoErrors()
        {
            var card = CardWith(new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.LessonStart, children: new[] { new EffectDef(EffectOp.AddStatus, status: StatusKind.Impression, value: 1) }));
            Assert.Empty(CardDefinitionValidator.Validate(card));
        }

        [Fact]
        public void Validate_TurnStartPersistentWithDrawCards_NoErrors()
        {
            var card = CardWith(new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.TurnStart, children: new[] { new EffectDef(EffectOp.DrawCards, value: 1) }));
            Assert.Empty(CardDefinitionValidator.Validate(card));
        }

        [Fact]
        public void Validate_ConditionSubjectNothing_ReturnsError()
        {
            var conditions = new[] { new ConditionDef(ConditionSubject.Nothing, Comparator.GreaterOrEqual, 1) };
            var face = new CardFace(new CostDef[0], conditions, new[] { new EffectDef(EffectOp.GainParam, value: 1) });
            var card = new CardDefinition(999, Plan.Logic, CardType.Active, Rarity.SR, 1, true, false, false, face, null);
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("条件が不正", errors[0]);
        }

        [Fact]
        public void Validate_CardPlanNothing_ReturnsError()
        {
            var face = new CardFace(new CostDef[0], new ConditionDef[0], new[] { new EffectDef(EffectOp.GainParam, value: 1) });
            var card = new CardDefinition(999, Plan.Nothing, CardType.Active, Rarity.SR, 1, true, false, false, face, null);
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("カードの区分が不正", errors[0]);
        }

        [Fact]
        public void LessonSetup_KindNothing_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LessonSetup(LessonKind.Nothing, ParamKind.Singing, 6, 30, 60));
        }
    }
}
