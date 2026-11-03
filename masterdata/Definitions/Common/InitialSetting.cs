using MasterMemory;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("initialSetting"), MessagePackObject(true)]
    public class InitialSetting : IValidatable<InitialSetting>
    {
        [PrimaryKey]
        public int Id { get; init; }

        public int GachaCurrency { get; init; }

        public int Gold { get; init; }

        public int SupportPoints { get; init; }

        void IValidatable<InitialSetting>.Validate(IValidator<InitialSetting> validator)
        {
            validator.Validate(x => x.Id == 1, "initialSetting: Id が 1 でない");
            validator.Validate(
                x => x.GachaCurrency >= 0 && x.Gold >= 0 && x.SupportPoints >= 0,
                "initialSetting: 量が負");
        }
    }
}
