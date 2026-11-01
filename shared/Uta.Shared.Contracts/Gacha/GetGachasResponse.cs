using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class GetGachasResponse
    {
        [SerializationConstructor]
        public GetGachasResponse(GachaStatusDto[] gachas)
        {
            Gachas = gachas;
        }

        [Key(0)]
        public GachaStatusDto[] Gachas { get; }
    }
}
