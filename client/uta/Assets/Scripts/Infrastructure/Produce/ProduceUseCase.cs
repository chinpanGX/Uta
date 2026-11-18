using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Player;
using Uta.Client.Application.Produce;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using VolTech.Client;
using VolTech.Server.Abstractions;

namespace Uta.Client.Infrastructure.Produce
{
    public sealed class ProduceUseCase : IProduceUseCase
    {
        private static readonly ErrorCode[] StartHandled = { ProduceErrors.InvalidFormation, ProduceErrors.AlreadyInProgress, ProduceErrors.InvalidArgument };
        private static readonly ErrorCode[] AbandonHandled = { ProduceErrors.NotInProgress, ProduceErrors.SessionDiscarded };
        private static readonly ErrorCode[] StepHandled =
        {
            ProduceErrors.StaleOperation, ProduceErrors.UnexpectedPhase, ProduceErrors.InvalidArgument,
            ProduceErrors.NotInProgress, ProduceErrors.SessionDiscarded,
        };

        private readonly IProduceService produce;
        private readonly ProduceSessionState session;
        private readonly IPlayerUseCase player;

        public ProduceUseCase(IProduceService produce, ProduceSessionState session, IPlayerUseCase player)
        {
            this.produce = produce;
            this.session = session;
            this.player = player;
        }

        public async UniTask<ProduceStartResultKind> StartAsync(int idolId, IReadOnlyList<string> supportCardUids, int scenarioId)
        {
            var uids = new string[supportCardUids.Count];
            for (var i = 0; i < uids.Length; i++)
            {
                uids[i] = supportCardUids[i];
            }

            var result = await produce.WithHandledCodes(StartHandled)
                .StartAsync(new StartProduceRequest(idolId, uids, 0, scenarioId)).ToResult();
            if (!result.IsError)
            {
                session.Set(ProduceMapper.ToStateResult(result.Value));
                return ProduceStartResultKind.Succeeded;
            }

            if (result.ErrorCode == ProduceErrors.InvalidFormation)
            {
                return ProduceStartResultKind.InvalidFormation;
            }

            if (result.ErrorCode == ProduceErrors.AlreadyInProgress)
            {
                // 手元のセッションはサーバーの進行中のものではない。残すとホームの「再開」が出ない
                session.Clear();
                await TryRefreshAsync();
                return ProduceStartResultKind.AlreadyInProgress;
            }

            if (result.ErrorCode == ProduceErrors.InvalidArgument)
            {
                return ProduceStartResultKind.InvalidArgument;
            }

            return ProduceStartResultKind.Failed;
        }

        public async UniTask<ProduceResumeResultKind> ResumeAsync()
        {
            var result = await produce.GetStateAsync().ToResult();
            if (result.IsError)
            {
                return ProduceResumeResultKind.Failed;
            }

            var state = ProduceMapper.ToStateResult(result.Value);
            if (state.Phase != ProducePhase.Nothing)
            {
                session.Set(state);
                return ProduceResumeResultKind.Resumed;
            }

            session.Clear();
            await TryRefreshAsync();
            return ProduceResumeResultKind.NotInProgress;
        }

        public async UniTask<bool> AbandonAsync()
        {
            var result = await produce.WithHandledCodes(AbandonHandled).AbandonAsync().ToResult();
            if (!result.IsError || result.ErrorCode == ProduceErrors.NotInProgress || result.ErrorCode == ProduceErrors.SessionDiscarded)
            {
                session.Clear();
                await TryRefreshAsync();
                return true;
            }

            return false;
        }

        public UniTask<ProduceStepResultKind> ChooseWeekActionAsync(int slotIndex, int optionIndex, ParamKind param)
        {
            return RunAsync(op => produce.WithHandledCodes(StepHandled)
                .ChooseWeekActionAsync(new ChooseWeekActionRequest(op, slotIndex, optionIndex, param)).ToResult());
        }

        public UniTask<ProduceStepResultKind> PickCardOfferAsync(int offerIndex)
        {
            return RunAsync(op => produce.WithHandledCodes(StepHandled)
                .ResolveCardOfferAsync(new ResolveCardOfferRequest(op, CardOfferActionKind.Pick, offerIndex)).ToResult());
        }

        public UniTask<ProduceStepResultKind> DeclineCardOfferAsync()
        {
            return RunAsync(op => produce.WithHandledCodes(StepHandled)
                .ResolveCardOfferAsync(new ResolveCardOfferRequest(op, CardOfferActionKind.Decline, 0)).ToResult());
        }

        public UniTask<ProduceStepResultKind> DecideExamRetryAsync(bool retry)
        {
            return RunAsync(op => produce.WithHandledCodes(StepHandled)
                .DecideExamRetryAsync(new DecideExamRetryRequest(op, retry)).ToResult());
        }

        public UniTask<ProduceStepResultKind> UseCardAsync(int cardInstanceId)
        {
            return RunAsync(op => produce.WithHandledCodes(StepHandled)
                .ExecuteLessonActionAsync(new ExecuteLessonActionRequest(op, LessonActionKind.UseCard, cardInstanceId, 0)).ToResult());
        }

        public UniTask<ProduceStepResultKind> UseDrinkAsync(int drinkSlotIndex)
        {
            return RunAsync(op => produce.WithHandledCodes(StepHandled)
                .ExecuteLessonActionAsync(new ExecuteLessonActionRequest(op, LessonActionKind.UseDrink, 0, drinkSlotIndex)).ToResult());
        }

        public UniTask<ProduceStepResultKind> SkipLessonTurnAsync()
        {
            return RunAsync(op => produce.WithHandledCodes(StepHandled)
                .ExecuteLessonActionAsync(new ExecuteLessonActionRequest(op, LessonActionKind.Skip, 0, 0)).ToResult());
        }

        public void MarkExamResultShown()
        {
            session.ExamResultShown = true;
        }

        public void ClearSession()
        {
            session.Clear();
        }

        private async UniTask<ProduceStepResultKind> RunAsync(Func<int, UniTask<ApiResult<ProduceStateResponse>>> call)
        {
            if (session.Current == null)
            {
                throw new InvalidOperationException("no active produce");
            }

            var result = await call(session.Current.OperationNumber);
            if (!result.IsError)
            {
                var next = ProduceMapper.ToStateResult(result.Value);
                session.Set(next);
                if (next.Phase == ProducePhase.Finished)
                {
                    await TryRefreshAsync();
                }

                return ProduceStepResultKind.Succeeded;
            }

            if (result.ErrorCode == ProduceErrors.StaleOperation || result.ErrorCode == ProduceErrors.UnexpectedPhase)
            {
                var latest = await produce.GetStateAsync().ToResult();
                if (latest.IsError)
                {
                    return ProduceStepResultKind.Failed;
                }

                var state = ProduceMapper.ToStateResult(latest.Value);
                if (state.Phase != ProducePhase.Nothing)
                {
                    session.Set(state);
                    return ProduceStepResultKind.Recovered;
                }

                session.Clear();
                await TryRefreshAsync();
                return ProduceStepResultKind.SessionEnded;
            }

            if (result.ErrorCode == ProduceErrors.NotInProgress || result.ErrorCode == ProduceErrors.SessionDiscarded)
            {
                session.Clear();
                await TryRefreshAsync();
                return ProduceStepResultKind.SessionEnded;
            }

            if (result.ErrorCode == ProduceErrors.InvalidArgument)
            {
                return ProduceStepResultKind.Rejected;
            }

            return ProduceStepResultKind.Failed;
        }

        /// <summary>RPC が成功した後の取り直し。失敗（InvalidOperationException。ApiCore が共通のダイアログを出し済み）は握りつぶす。成功で true。</summary>
        private async UniTask<bool> TryRefreshAsync()
        {
            try
            {
                await player.RefreshAsync();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}
