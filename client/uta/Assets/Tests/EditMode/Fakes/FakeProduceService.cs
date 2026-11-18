using System;
using System.Collections.Generic;
using System.Threading;
using Grpc.Core;
using MagicOnion;
using Uta.Shared.Contracts.Produce;
using VolTech.Client;
using VolTech.Server.Abstractions;

namespace Uta.Client.Tests
{
    /// <summary>
    /// 通信する代わりに、積んだ結果を返す IProduceService。呼び出しは CallLog に記録する。
    /// WithHandledCodes（WithHeaders）は自分を返す。ApiCore を通らないので、処理済みのコードの扱いは確かめない。
    /// </summary>
    public sealed class FakeProduceService : IProduceService
    {
        private readonly CallLog log;

        /// <summary>log が null なら記録しない。</summary>
        public FakeProduceService(CallLog log = null)
        {
            this.log = log;
        }

        /// <summary>StartAsync・GetStateAsync・5 つの操作が、呼ばれた順に共有して取り出す。空なら InvalidOperationException。</summary>
        public Queue<Func<UnaryResult<ProduceStateResponse>>> StateResults { get; } = new Queue<Func<UnaryResult<ProduceStateResponse>>>();

        /// <summary>AbandonAsync が取り出す。空なら InvalidOperationException。</summary>
        public Queue<Func<UnaryResult<ProduceResultResponse>>> AbandonResults { get; } = new Queue<Func<UnaryResult<ProduceResultResponse>>>();

        public static Func<UnaryResult<T>> Ok<T>(T value)
        {
            return () => new UnaryResult<T>(value);
        }

        public static Func<UnaryResult<T>> Fail<T>(ErrorCode code)
        {
            return () => FakeRpcErrors.ApiFailureResult<T>(code);
        }

        public UnaryResult<ProduceStateResponse> StartAsync(StartProduceRequest request)
        {
            Record("Start:" + request.IdolMasterId + ":" + string.Join(",", request.SupportCardUniqueIds) + ":" + request.ScenarioMasterId);
            return NextState();
        }

        public UnaryResult<ProduceStateResponse> GetStateAsync()
        {
            Record("GetState");
            return NextState();
        }

        public UnaryResult<ProduceResultResponse> AbandonAsync()
        {
            Record("Abandon");
            if (AbandonResults.Count == 0)
            {
                throw new InvalidOperationException("FakeProduceService: no abandon result");
            }

            return AbandonResults.Dequeue()();
        }

        public UnaryResult<ProduceStateResponse> ChooseWeekActionAsync(ChooseWeekActionRequest request)
        {
            Record("ChooseWeekAction:" + request.OperationNumber + "," + request.SlotIndex + "," + request.OptionIndex + "," + request.Param);
            return NextState();
        }

        public UnaryResult<ProduceStateResponse> ResolveCardOfferAsync(ResolveCardOfferRequest request)
        {
            Record("ResolveCardOffer:" + request.OperationNumber + "," + request.Kind + "," + request.OfferIndex);
            return NextState();
        }

        public UnaryResult<ProduceStateResponse> DecideExamRetryAsync(DecideExamRetryRequest request)
        {
            Record("DecideExamRetry:" + request.OperationNumber + "," + request.Retry);
            return NextState();
        }

        public UnaryResult<ProduceStateResponse> ExecuteLessonActionAsync(ExecuteLessonActionRequest request)
        {
            Record("ExecuteLessonAction:" + request.OperationNumber + "," + request.Kind + "," + request.CardInstanceId + "," + request.DrinkSlotIndex);
            return NextState();
        }

        public IProduceService WithOptions(CallOptions option)
        {
            return this;
        }

        public IProduceService WithHeaders(Metadata headers)
        {
            return this;
        }

        public IProduceService WithDeadline(DateTime deadline)
        {
            return this;
        }

        public IProduceService WithCancellationToken(CancellationToken cancellationToken)
        {
            return this;
        }

        public IProduceService WithHost(string host)
        {
            throw new NotSupportedException();
        }

        private UnaryResult<ProduceStateResponse> NextState()
        {
            if (StateResults.Count == 0)
            {
                throw new InvalidOperationException("FakeProduceService: no state result");
            }

            return StateResults.Dequeue()();
        }

        private void Record(string entry)
        {
            if (log != null)
            {
                log.Add(entry);
            }
        }
    }
}
