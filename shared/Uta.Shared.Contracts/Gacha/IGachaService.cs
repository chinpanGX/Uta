using MagicOnion;
using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Gacha
{
    /// <summary>
    /// ガチャの一覧・抽選・天井の交換。認証あり。GetGachasAsync だけ副作用なし。
    /// </summary>
    public interface IGachaService : IService<IGachaService>
    {
        [NoSideEffects]
        UnaryResult<GetGachasResponse> GetGachasAsync();

        UnaryResult<DrawResponse> DrawAsync(DrawRequest request);

        UnaryResult<ExchangeResponse> ExchangeAsync(ExchangeRequest request);
    }
}
