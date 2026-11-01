using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Gacha
{
    public static class GachaErrors
    {
        public static readonly ErrorCode NotEnoughCurrency = new ErrorCode("uta", "GACHA_NOT_ENOUGH_CURRENCY");
        public static readonly ErrorCode NotEnoughPoints = new ErrorCode("uta", "GACHA_NOT_ENOUGH_POINTS");
        public static readonly ErrorCode NotFound = new ErrorCode("uta", "GACHA_NOT_FOUND");
        public static readonly ErrorCode InvalidCount = new ErrorCode("uta", "GACHA_INVALID_COUNT");
        public static readonly ErrorCode CostMismatch = new ErrorCode("uta", "GACHA_COST_MISMATCH");
    }
}
