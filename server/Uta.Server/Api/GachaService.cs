using MagicOnion;
using MagicOnion.Server;
using Uta.Server.Application.Gacha;
using Uta.Shared.Contracts.Gacha;
using VolTech.Server;
using VolTech.Server.Abstractions;
using VolTech.Server.Transaction;

// Contracts は Nullable なし（netstandard2.1）、サーバーは Nullable 有効。IService<T> の With* の null 許容性が合わず CS8644 になる
#pragma warning disable CS8644

namespace Uta.Server.Api;

[UtaService]
public sealed class GachaService(ITransactions transactions, GetGachasUseCase getGachas, DrawUseCase draw, ExchangeUseCase exchange)
    : ServiceBase<IGachaService>, IGachaService
{
    public async UnaryResult<GetGachasResponse> GetGachasAsync()
    {
        var playerId = Context.GetAuthContext().PlayerId;
        return await transactions.RunAsync(Context, async tx =>
        {
            var gachas = await getGachas.ExecuteAsync(tx, playerId);
            return new GetGachasResponse(gachas.Select(x => new GachaStatusDto(x.GachaId, x.ExchangePoints)).ToArray());
        });
    }

    public async UnaryResult<DrawResponse> DrawAsync(DrawRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var requestId = Context.CallContext.RequestHeaders.GetValue(ApiHeaders.RequestId)!;   // 無い・形が違うときは Transactions が本体の前に INVALID_ARGUMENT を返す
        return await transactions.RunAsync(Context, async tx =>
        {
            var result = await draw.ExecuteAsync(tx, playerId, requestId, request.GachaId, request.Count, request.ExpectedCost);
            return new DrawResponse(result.Results.Select(ToDto).ToArray(), result.GachaCurrency, result.ExchangePoints);
        });
    }

    public async UnaryResult<ExchangeResponse> ExchangeAsync(ExchangeRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var requestId = Context.CallContext.RequestHeaders.GetValue(ApiHeaders.RequestId)!;
        return await transactions.RunAsync(Context, async tx =>
        {
            var result = await exchange.ExecuteAsync(tx, playerId, requestId, request.GachaId, request.CeilingItemOrder);
            return new ExchangeResponse(ToDto(result.Result), result.ExchangePoints);
        });
    }

    private static DrawResultDto ToDto(GachaResult x) => new DrawResultDto(
        x.Slot, (ItemKind)(int)x.Kind, x.ItemId, (GachaOutcome)(int)x.Outcome, x.Shards, x.SupportPoints, x.SupportCardUid?.ToString() ?? "");
}
