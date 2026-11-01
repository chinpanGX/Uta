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
        public void Validate_GainParamByShield_ReturnsError()
        {
            var card = CardWith(new EffectDef(EffectOp.GainParamByShield, value: 50));
            var errors = CardDefinitionValidator.Validate(card);
            Assert.Single(errors);
            Assert.Contains("未対応の効果", errors[0]);
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
