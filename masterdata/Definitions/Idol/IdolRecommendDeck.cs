using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("idolRecommendDeck"), MessagePackObject(true)]
    public class IdolRecommendDeck : IValidatable<IdolRecommendDeck>
    {
        [PrimaryKey(keyOrder: 0)]
        public RecommendKind Recommend { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Order { get; init; }

        [Exists(typeof(Card), nameof(Card.CardId))]
        public int CardId { get; init; }

        void IValidatable<IdolRecommendDeck>.Validate(IValidator<IdolRecommendDeck> validator)
        {
            validator.Validate(x => x.Recommend != RecommendKind.Nothing, "idolRecommendDeck: Recommend が Nothing");
            validator.Validate(x => x.Order >= 1, "idolRecommendDeck: Order が 1 未満");
        }
    }
}
