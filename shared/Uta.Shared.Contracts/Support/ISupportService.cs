using MagicOnion;
using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Support
{
    /// <summary>
    /// サポートカードのレベル上げ・上限解放。認証あり。
    /// </summary>
    public interface ISupportService : IService<ISupportService>
    {
        UnaryResult<LevelUpResponse> LevelUpAsync(LevelUpRequest request);

        UnaryResult<LimitBreakResponse> LimitBreakAsync(LimitBreakRequest request);
    }
}
