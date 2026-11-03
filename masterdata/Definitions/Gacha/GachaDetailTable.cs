using MasterMemory;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("gachaDetailTable"), MessagePackObject(true)]
    public class GachaDetailTable
    {
        [PrimaryKey]
        public int GachaTableId { get; init; }
    }
}
