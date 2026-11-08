using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("card"), MessagePackObject(true)]
    public class Card : IValidatable<Card>
    {
        [PrimaryKey]
        public int CardId { get; init; }

        [Unique]
        public string Name { get; init; } = "";

        public Plan Plan { get; init; }

        public CardType Type { get; init; }

        public Rarity Rarity { get; init; }

        public int UnlockLv { get; init; }

        public bool OnceInLesson { get; init; }

        public bool Unique { get; init; }

        public bool StartInHand { get; init; }

        void IValidatable<Card>.Validate(IValidator<Card> validator)
        {
            validator.Validate(x => !string.IsNullOrWhiteSpace(x.Name), "card: Name が空");
            validator.Validate(x => x.Plan != Plan.Nothing, "card: Plan が Nothing");
            validator.Validate(x => x.Type != CardType.Nothing, "card: Type が Nothing");
            validator.Validate(x => x.Rarity != Rarity.Nothing, "card: Rarity が Nothing");
            validator.Validate(x => x.UnlockLv >= 1, "card: UnlockLv が 1 未満");
        }
    }
}
