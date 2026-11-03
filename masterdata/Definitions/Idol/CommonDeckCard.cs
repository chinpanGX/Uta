using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("commonDeckCard"), MessagePackObject(true)]
    public class CommonDeckCard : IValidatable<CommonDeckCard>
    {
        [PrimaryKey]
        public int Order { get; init; }

        [Exists(typeof(Card), nameof(Card.CardId))]
        public int CardId { get; init; }

        void IValidatable<CommonDeckCard>.Validate(IValidator<CommonDeckCard> validator)
        {
            validator.Validate(x => x.Order >= 1, "commonDeckCard: Order が 1 未満");
        }
    }
}
