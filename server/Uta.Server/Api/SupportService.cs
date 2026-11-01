using MagicOnion;
using MagicOnion.Server;
using Uta.Server.Application.Support;
using Uta.Shared.Contracts.Support;
using VolTech.Server;
using VolTech.Server.Transaction;

// Contracts は Nullable なし（netstandard2.1）、サーバーは Nullable 有効。IService<T> の With* の null 許容性が合わず CS8644 になる
#pragma warning disable CS8644

namespace Uta.Server.Api;

[UtaService]
public sealed class SupportService(ITransactions transactions, LevelUpUseCase levelUp, LimitBreakUseCase limitBreak)
    : ServiceBase<ISupportService>, ISupportService
{
    public async UnaryResult<LevelUpResponse> LevelUpAsync(LevelUpRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        return await transactions.RunAsync(Context, async tx =>
        {
            var result = await levelUp.ExecuteAsync(tx, playerId, ParseUid(request.SupportCardUid), request.TargetLevel);
            return new LevelUpResponse(result.Level, result.SupportPoints, result.Gold);
        });
    }

    public async UnaryResult<LimitBreakResponse> LimitBreakAsync(LimitBreakRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        return await transactions.RunAsync(Context, async tx =>
        {
            var result = await limitBreak.ExecuteAsync(tx, playerId, ParseUid(request.SupportCardUid), ParseUid(request.MaterialUid));
            return new LimitBreakResponse(result.LimitBreak, result.MaxLevel);
        });
    }

    /// <summary>形が違う文字列は Ulid.Empty（どの所持IDとも一致しない）にして、NOT_OWNED / INVALID_MATERIAL に落とす。</summary>
    private static Ulid ParseUid(string value) => Ulid.TryParse(value, out var uid) ? uid : Ulid.Empty;
}
