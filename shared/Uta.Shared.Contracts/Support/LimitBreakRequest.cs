using MessagePack;

namespace Uta.Shared.Contracts.Support
{
    [MessagePackObject]
    public sealed class LimitBreakRequest
    {
        [SerializationConstructor]
        public LimitBreakRequest(string supportCardUid, string materialUid)
        {
            SupportCardUid = supportCardUid;
            MaterialUid = materialUid;
        }

        /// <summary>解放するカードの所持ID。</summary>
        [Key(0)]
        public string SupportCardUid { get; }

        /// <summary>素材の所持ID。</summary>
        [Key(1)]
        public string MaterialUid { get; }
    }
}
