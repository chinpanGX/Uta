using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("cardFace"), MessagePackObject(true)]
    public class CardFace : IValidatable<CardFace>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Card), nameof(Card.CardId))]
        public int CardId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public CardFaceKind Face { get; init; }

        [Exists(typeof(Effect), nameof(Effect.EffectGroupId))]
        public int EffectGroupId { get; init; }

        [Exists(typeof(Condition), nameof(Condition.ConditionGroupId), AllowEmpty = true)]
        public int UseConditionGroupId { get; init; }

        void IValidatable<CardFace>.Validate(IValidator<CardFace> validator)
        {
            validator.Validate(x => x.Face != CardFaceKind.Nothing, "cardFace: Face が Nothing");
            validator.Validate(x => x.EffectGroupId != 0, "cardFace: EffectGroupId が 0");
        }
    }
}
