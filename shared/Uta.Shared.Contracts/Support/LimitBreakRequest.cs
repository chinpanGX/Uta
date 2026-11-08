using MessagePack;

namespace Uta.Shared.Contracts.Support
{
    [MessagePackObject]
    public sealed class LimitBreakRequest
    {
        [SerializationConstructor]
        public LimitBreakRequest(string supportCardUniqueId, string materialUniqueId)
        {
            SupportCardUniqueId = supportCardUniqueId;
            MaterialUniqueId = materialUniqueId;
        }

        /// <summary>解放するカードの所持ID。</summary>
        [Key(0)]
        public string SupportCardUniqueId { get; }

        /// <summary>素材の所持ID。</summary>
        [Key(1)]
        public string MaterialUniqueId { get; }
    }
}
