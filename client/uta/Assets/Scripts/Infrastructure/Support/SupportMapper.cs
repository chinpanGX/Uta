using Uta.Client.Application.Support;
using Uta.Shared.Contracts.Support;

namespace Uta.Client.Infrastructure.Support
{
    /// <summary>DTO → Application の型。</summary>
    public static class SupportMapper
    {
        public static LevelUpResult ToLevelUpResult(LevelUpResponse response)
        {
            return LevelUpResult.Success(response.Level, response.SupportPoints, response.Gold);
        }

        public static LimitBreakResult ToLimitBreakResult(LimitBreakResponse response)
        {
            return LimitBreakResult.Success(response.LimitBreak, response.MaxLevel);
        }
    }
}
