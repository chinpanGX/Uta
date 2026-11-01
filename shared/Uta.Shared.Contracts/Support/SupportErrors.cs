using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Support
{
    public static class SupportErrors
    {
        public static readonly ErrorCode InvalidTargetLevel = new ErrorCode("uta", "SUPPORT_INVALID_TARGET_LEVEL");
        public static readonly ErrorCode NotEnoughPoints = new ErrorCode("uta", "SUPPORT_NOT_ENOUGH_POINTS");
        public static readonly ErrorCode MaxLimitBreak = new ErrorCode("uta", "SUPPORT_MAX_LIMIT_BREAK");
        public static readonly ErrorCode NotOwned = new ErrorCode("uta", "SUPPORT_NOT_OWNED");
        public static readonly ErrorCode InvalidMaterial = new ErrorCode("uta", "SUPPORT_INVALID_MATERIAL");
    }
}
