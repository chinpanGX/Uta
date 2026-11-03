using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("initialSupportCard"), MessagePackObject(true)]
    public class InitialSupportCard : IValidatable<InitialSupportCard>
    {
        [PrimaryKey]
        public int Order { get; init; }

        [Exists(typeof(SupportCard), nameof(SupportCard.SupportCardId))]
        public int SupportCardId { get; init; }

        void IValidatable<InitialSupportCard>.Validate(IValidator<InitialSupportCard> validator)
        {
            validator.Validate(x => x.Order >= 1, "initialSupportCard: Order が 1 未満");
        }
    }
}
