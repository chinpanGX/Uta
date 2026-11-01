using MessagePack;

namespace Uta.Shared.Contracts.Gacha
{
    [MessagePackObject]
    public sealed class DrawRequest
    {
        [SerializationConstructor]
        public DrawRequest(int gachaId, int count, int expectedCost)
        {
            GachaId = gachaId;
            Count = count;
            ExpectedCost = expectedCost;
        }

        [Key(0)]
        public int GachaId { get; }

        /// <summary>1 か 10。</summary>
        [Key(1)]
        public int Count { get; }

        /// <summary>画面に出した価格。</summary>
        [Key(2)]
        public int ExpectedCost { get; }
    }
}
