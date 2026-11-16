using Uta.Shared.Domain.Lesson;

namespace Uta.Client.Application.Produce
{
    public sealed class ProduceClassOption
    {
        public ProduceClassOption(int index, int hpCost, ParamKind param, int paramGain, bool givesCard, bool selectable)
        {
            Index = index;
            HpCost = hpCost;
            Param = param;
            ParamGain = paramGain;
            GivesCard = givesCard;
            Selectable = selectable;
        }

        public int Index { get; }
        public int HpCost { get; }
        public ParamKind Param { get; }
        public int ParamGain { get; }
        public bool GivesCard { get; }
        public bool Selectable { get; }
    }
}
