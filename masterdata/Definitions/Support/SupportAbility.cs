using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("supportAbility"), MessagePackObject(true)]
    public class SupportAbility : IValidatable<SupportAbility>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(SupportCard), nameof(SupportCard.SupportCardId))]
        public int SupportCardId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Order { get; init; }

        public SupportAbilityKind Kind { get; init; }

        public ParamKind Param { get; init; }

        public int ValueAtLv1 { get; init; }

        public int ValueAtMax { get; init; }

        void IValidatable<SupportAbility>.Validate(IValidator<SupportAbility> validator)
        {
            validator.Validate(x => x.Kind != SupportAbilityKind.Nothing, "supportAbility: Kind が Nothing");
            validator.Validate(
                x => x.Kind == SupportAbilityKind.MaxHp ? x.Param == ParamKind.Nothing : x.Param != ParamKind.Nothing,
                "supportAbility: MaxHp の Param が Nothing でないか、MaxHp 以外の Param が Nothing");
            validator.Validate(x => x.ValueAtLv1 >= 0, "supportAbility: ValueAtLv1 が負");
            validator.Validate(x => x.ValueAtMax >= x.ValueAtLv1, "supportAbility: ValueAtMax が ValueAtLv1 未満");
            validator.Validate(x => x.Order >= 1, "supportAbility: Order が 1 未満");
        }
    }
}
