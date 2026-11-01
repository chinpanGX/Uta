using MessagePack;

namespace Uta.Shared.Contracts.Player
{
    [MessagePackObject]
    public sealed class SignUpResponse
    {
        [SerializationConstructor]
        public SignUpResponse(bool created)
        {
            Created = created;
        }

        /// <summary>今回作ったか。2 回目以降は false。</summary>
        [Key(0)]
        public bool Created { get; }
    }
}
