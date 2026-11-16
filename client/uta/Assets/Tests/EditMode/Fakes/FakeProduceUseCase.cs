using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Produce;
using Uta.Shared.Domain.Lesson;

namespace Uta.Client.Tests
{
    /// <summary>
    /// IProduceUseCase の偽（Presenter・ProduceFlow・ProduceMenu のテスト用）。
    /// 偽そのものはセッション・リポジトリに書かない。本物の具象が行う副作用は、テストが積む Func の中で再現する。
    /// </summary>
    public sealed class FakeProduceUseCase : IProduceUseCase
    {
        private readonly CallLog log;
        private readonly ProduceSessionState session;

        /// <summary>log が null なら記録しない。session が渡されていれば、MarkExamResultShown・ClearSession がそれに書く。</summary>
        public FakeProduceUseCase(CallLog log = null, ProduceSessionState session = null)
        {
            this.log = log;
            this.session = session;
        }

        /// <summary>StartAsync の 1 回ごとの振る舞い。空なら InvalidOperationException。</summary>
        public Queue<Func<UniTask<ProduceStartResultKind>>> StartResults { get; } = new Queue<Func<UniTask<ProduceStartResultKind>>>();

        /// <summary>ResumeAsync の 1 回ごとの振る舞い。空なら Resumed。</summary>
        public Queue<Func<UniTask<ProduceResumeResultKind>>> ResumeResults { get; } = new Queue<Func<UniTask<ProduceResumeResultKind>>>();

        /// <summary>AbandonAsync の 1 回ごとの振る舞い。空なら true。</summary>
        public Queue<Func<UniTask<bool>>> AbandonResults { get; } = new Queue<Func<UniTask<bool>>>();

        /// <summary>7 つの操作が共有して取り出す。空なら Succeeded。</summary>
        public Queue<Func<UniTask<ProduceStepResultKind>>> StepResults { get; } = new Queue<Func<UniTask<ProduceStepResultKind>>>();

        /// <summary>log に記録した文字列と同じもの。</summary>
        public List<string> Calls { get; } = new List<string>();

        public UniTask<ProduceStartResultKind> StartAsync(int idolId, IReadOnlyList<string> supportCardUids, int scenarioId)
        {
            Record("Start:" + idolId + ":" + string.Join(",", supportCardUids) + ":" + scenarioId);
            if (StartResults.Count == 0)
            {
                throw new InvalidOperationException("FakeProduceUseCase: no start result");
            }

            return StartResults.Dequeue()();
        }

        public UniTask<ProduceResumeResultKind> ResumeAsync()
        {
            Record("Resume");
            if (ResumeResults.Count == 0)
            {
                return UniTask.FromResult(ProduceResumeResultKind.Resumed);
            }

            return ResumeResults.Dequeue()();
        }

        public UniTask<bool> AbandonAsync()
        {
            Record("Abandon");
            if (AbandonResults.Count == 0)
            {
                return UniTask.FromResult(true);
            }

            return AbandonResults.Dequeue()();
        }

        public UniTask<ProduceStepResultKind> ChooseWeekActionAsync(int slotIndex, int optionIndex, ParamKind param)
        {
            return Step("ChooseWeekAction:" + slotIndex + "," + optionIndex + "," + param);
        }

        public UniTask<ProduceStepResultKind> PickCardOfferAsync(int offerIndex)
        {
            return Step("PickCardOffer:" + offerIndex);
        }

        public UniTask<ProduceStepResultKind> DeclineCardOfferAsync()
        {
            return Step("DeclineCardOffer");
        }

        public UniTask<ProduceStepResultKind> DecideExamRetryAsync(bool retry)
        {
            return Step("DecideExamRetry:" + retry);
        }

        public UniTask<ProduceStepResultKind> UseCardAsync(int cardInstanceId)
        {
            return Step("UseCard:" + cardInstanceId);
        }

        public UniTask<ProduceStepResultKind> UseDrinkAsync(int drinkSlotIndex)
        {
            return Step("UseDrink:" + drinkSlotIndex);
        }

        public UniTask<ProduceStepResultKind> SkipLessonTurnAsync()
        {
            return Step("SkipLessonTurn");
        }

        public void MarkExamResultShown()
        {
            Record("MarkExamResultShown");
            if (session != null)
            {
                session.ExamResultShown = true;
            }
        }

        public void ClearSession()
        {
            Record("ClearSession");
            if (session != null)
            {
                session.Clear();
            }
        }

        private UniTask<ProduceStepResultKind> Step(string entry)
        {
            Record(entry);
            if (StepResults.Count == 0)
            {
                return UniTask.FromResult(ProduceStepResultKind.Succeeded);
            }

            return StepResults.Dequeue()();
        }

        private void Record(string entry)
        {
            Calls.Add(entry);
            if (log != null)
            {
                log.Add(entry);
            }
        }
    }
}
