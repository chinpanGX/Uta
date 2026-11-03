using MasterMemory;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("scoreBonusSetting"), MessagePackObject(true)]
    public class ScoreBonusSetting : IValidatable<ScoreBonusSetting>
    {
        [PrimaryKey]
        public int Id { get; init; }

        public int APerThousand { get; init; }

        public int BFirst { get; init; }

        public int BSecond { get; init; }

        public int BThird { get; init; }

        void IValidatable<ScoreBonusSetting>.Validate(IValidator<ScoreBonusSetting> validator)
        {
            validator.Validate(x => x.Id == 1, "scoreBonusSetting: Id が 1 でない");
            validator.Validate(
                x => x.APerThousand >= 0 && x.BFirst >= 0 && x.BSecond >= 0 && x.BThird >= 0,
                "scoreBonusSetting: 係数が負");
        }
    }
}
