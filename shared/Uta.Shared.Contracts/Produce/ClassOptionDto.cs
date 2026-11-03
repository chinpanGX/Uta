using MessagePack;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ClassOptionDto
    {
        [SerializationConstructor]
        public ClassOptionDto(int index, int hpCost, ParamKind param, int paramGain, bool givesCard, bool selectable)
        {
            Index = index;
            HpCost = hpCost;
            Param = param;
            ParamGain = paramGain;
            GivesCard = givesCard;
            Selectable = selectable;
        }

        [Key(0)]
        public int Index { get; }

        [Key(1)]
        public int HpCost { get; }

        [Key(2)]
        public ParamKind Param { get; }

        [Key(3)]
        public int ParamGain { get; }

        [Key(4)]
        public bool GivesCard { get; }

        [Key(5)]
        public bool Selectable { get; }
    }
}
