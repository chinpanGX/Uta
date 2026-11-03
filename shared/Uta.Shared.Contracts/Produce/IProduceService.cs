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

        UnaryResult<ProduceStateResponse> ChooseWeekActionAsync(ChooseWeekActionRequest request);

        UnaryResult<ProduceStateResponse> ResolveCardOfferAsync(ResolveCardOfferRequest request);

        UnaryResult<ProduceStateResponse> DecideExamRetryAsync(DecideExamRetryRequest request);

        UnaryResult<ProduceStateResponse> ExecuteLessonActionAsync(ExecuteLessonActionRequest request);
    }
}
