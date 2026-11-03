using MagicOnion;
using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Produce
{
    /// <summary>
    /// プロデュース。認証あり。
    /// </summary>
    public interface IProduceService : IService<IProduceService>
    {
        UnaryResult<ProduceStateResponse> StartAsync(StartProduceRequest request);

        [NoSideEffects]
        UnaryResult<ProduceStateResponse> GetStateAsync();

        UnaryResult<ProduceResultResponse> AbandonAsync();
    }
}
