using MagicOnion;
using MagicOnion.Server;
using Uta.Server.Application.Produce;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Domain.Produce;
using VolTech.Server;
using VolTech.Server.Abstractions;
using VolTech.Server.Transaction;

// Contracts は Nullable なし（netstandard2.1）、サーバーは Nullable 有効。IService<T> の With* の null 許容性が合わず CS8644 になる
#pragma warning disable CS8644

namespace Uta.Server.Api;

[GameServiceFilter]
public sealed class ProduceService(
    ITransactions transactions, StartProduceUseCase start, GetProduceStateUseCase getState, AbandonProduceUseCase abandon,
    ProduceOperationUseCase operation)
    : ServiceBase<IProduceService>, IProduceService
{
    public async UnaryResult<ProduceStateResponse> StartAsync(StartProduceRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var appVersion = AppVersionHeader.Require(Context);
        return await transactions.RunAsync(Context, async tx =>
        {
            var view = await start.ExecuteAsync(
                tx, playerId, appVersion, request.IdolMasterId, request.SupportCardUniqueIds ?? [], request.RentalCandidateMasterId, request.ScenarioMasterId);
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

    public async UnaryResult<ProduceStateResponse> ChooseWeekActionAsync(ChooseWeekActionRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var appVersion = AppVersionHeader.Require(Context);
        var command = new ProduceCommand(OperationKind.ChooseWeekAction, request.SlotIndex, request.OptionIndex, (int)request.Param);
        return await RunOperationAsync(playerId, appVersion, request.OperationNumber, command);
    }

    public async UnaryResult<ProduceStateResponse> ResolveCardOfferAsync(ResolveCardOfferRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var appVersion = AppVersionHeader.Require(Context);
        var command = request.Kind switch
        {
            CardOfferActionKind.Pick => ProduceCommand.OfferPick(request.OfferIndex),
            CardOfferActionKind.Reroll => ProduceCommand.OfferReroll(),
            CardOfferActionKind.Remove => ProduceCommand.OfferRemove(request.OfferIndex),
            CardOfferActionKind.Decline => ProduceCommand.OfferDecline(),
            _ => throw new BusinessException(ProduceErrors.InvalidArgument, "invalid kind"),
        };
        return await RunOperationAsync(playerId, appVersion, request.OperationNumber, command);
    }

    public async UnaryResult<ProduceStateResponse> DecideExamRetryAsync(DecideExamRetryRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var appVersion = AppVersionHeader.Require(Context);
        var command = request.Retry ? ProduceCommand.ExamRetry() : ProduceCommand.ExamNoRetry();
        return await RunOperationAsync(playerId, appVersion, request.OperationNumber, command);
    }

    public async UnaryResult<ProduceStateResponse> ExecuteLessonActionAsync(ExecuteLessonActionRequest request)
    {
        var playerId = Context.GetAuthContext().PlayerId;
        var appVersion = AppVersionHeader.Require(Context);
        var command = request.Kind switch
        {
            LessonActionKind.UseCard => ProduceCommand.UseCard(request.CardInstanceId),
            LessonActionKind.UseDrink => ProduceCommand.UseDrink(request.DrinkSlotIndex),
            LessonActionKind.Skip => ProduceCommand.Skip(),
            _ => throw new BusinessException(ProduceErrors.InvalidArgument, "invalid kind"),
        };
        return await RunOperationAsync(playerId, appVersion, request.OperationNumber, command);
    }

    private Task<ProduceStateResponse> RunOperationAsync(Ulid playerId, int appVersion, int operationNumber, ProduceCommand command) =>
        transactions.RunAsync(Context, async tx =>
            ProduceResponseMapper.ToStateResponse(await operation.ExecuteAsync(tx, playerId, appVersion, operationNumber, command)));
}
