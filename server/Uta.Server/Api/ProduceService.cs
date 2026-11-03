using MagicOnion;
using MagicOnion.Server;
using Uta.Server.Application.Produce;
using Uta.Shared.Contracts.Produce;
using VolTech.Server;
using VolTech.Server.Transaction;

// Contracts は Nullable なし（netstandard2.1）、サーバーは Nullable 有効。IService<T> の With* の null 許容性が合わず CS8644 になる
#pragma warning disable CS8644

namespace Uta.Server.Api;

[UtaService]
public sealed class ProduceService(
    ITransactions transactions, StartProduceUseCase start, GetProduceStateUseCase getState, AbandonProduceUseCase abandon)
    : ServiceBase<IProduceService>, IProduceService
{
    public async UnaryResult<ProduceStateResponse> StartAsync(StartProduceRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var appVersion = AppVersionHeader.Require(Context);
        return await transactions.RunAsync(Context, async tx =>
        {
            var view = await start.ExecuteAsync(
                tx, playerId, appVersion, request.IdolId, request.SupportCardUids ?? [], request.RentalCandidateId, request.ScenarioId);
            return ProduceResponseMapper.ToStateResponse(view);
        });
    }

    public async UnaryResult<ProduceStateResponse> GetStateAsync()
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var appVersion = AppVersionHeader.Require(Context);
        return await transactions.RunAsync(Context, async tx =>
            ProduceResponseMapper.ToStateResponse(await getState.ExecuteAsync(tx, playerId, appVersion)));
    }

    public async UnaryResult<ProduceResultResponse> AbandonAsync()
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var appVersion = AppVersionHeader.Require(Context);
        return await transactions.RunAsync(Context, async tx =>
            ProduceResponseMapper.ToResultResponse(await abandon.ExecuteAsync(tx, playerId, appVersion)));
    }
}
