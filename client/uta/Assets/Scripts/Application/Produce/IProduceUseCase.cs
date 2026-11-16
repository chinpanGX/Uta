using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Shared.Domain.Lesson;

namespace Uta.Client.Application.Produce
{
    /// <summary>Contracts の IProduceService と同じ単位。メソッドは Presenter の操作の単位（ADR 0022）。</summary>
    public interface IProduceUseCase
    {
        UniTask<ProduceStartResultKind> StartAsync(int idolId, IReadOnlyList<string> supportCardUids, int scenarioId);

        UniTask<ProduceResumeResultKind> ResumeAsync();

        /// <summary>セッションが無くなったら true（放棄した、またはもう無かった）。残っていれば false。</summary>
        UniTask<bool> AbandonAsync();

        UniTask<ProduceStepResultKind> ChooseWeekActionAsync(int slotIndex, int optionIndex, ParamKind param);

        UniTask<ProduceStepResultKind> PickCardOfferAsync(int offerIndex);

        UniTask<ProduceStepResultKind> DeclineCardOfferAsync();

        UniTask<ProduceStepResultKind> DecideExamRetryAsync(bool retry);

        UniTask<ProduceStepResultKind> UseCardAsync(int cardInstanceId);

        UniTask<ProduceStepResultKind> UseDrinkAsync(int drinkSlotIndex);

        UniTask<ProduceStepResultKind> SkipLessonTurnAsync();

        /// <summary>試験結果の画面を出した印（session.ExamResultShown = true）。</summary>
        void MarkExamResultShown();

        /// <summary>session.Clear()。プロデュース結果の「ホームへ」。通信しない。</summary>
        void ClearSession();
    }
}
