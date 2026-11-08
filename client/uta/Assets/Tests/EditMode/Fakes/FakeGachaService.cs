using System;
using System.Collections.Generic;
using System.Threading;
using Grpc.Core;
using MagicOnion;
using Uta.Shared.Contracts.Gacha;
using VolTech.Client;
using VolTech.Server.Abstractions;

namespace Uta.Client.Tests
{
    /// <summary>通信する代わりに、積んだ結果を返す IGachaService。呼び出しは Calls に記録する。</summary>
    public sealed class FakeGachaService : IGachaService
    {
        public readonly Queue<Func<UnaryResult<GetGachasResponse>>> GetGachasResults = new Queue<Func<UnaryResult<GetGachasResponse>>>();
        public readonly Queue<Func<UnaryResult<DrawResponse>>> DrawResults = new Queue<Func<UnaryResult<DrawResponse>>>();
        public readonly List<string> Calls = new List<string>();

        /// <summary>最後に WithHeaders へ渡された Metadata（WithHandledCodes のコードの指定が入る）。</summary>
        public Metadata LastHeaders { get; private set; }

        public static Func<UnaryResult<T>> Ok<T>(T value)
        {
            return () => new UnaryResult<T>(value);
        }

        public static Func<UnaryResult<T>> Fail<T>(ErrorCode code)
        {
            return () => FakeRpcErrors.ApiFailureResult<T>(code);
        }

        public UnaryResult<GetGachasResponse> GetGachasAsync()
        {
            Calls.Add("GetGachas");
            return Next(GetGachasResults)();
        }

        public UnaryResult<DrawResponse> DrawAsync(DrawRequest request)
        {
            Calls.Add("Draw:" + request.GachaMasterId + "," + request.Count + "," + request.ExpectedCost);
            return Next(DrawResults)();
        }

        public UnaryResult<ExchangeResponse> ExchangeAsync(ExchangeRequest request)
        {
            throw new NotSupportedException();
        }

        public IGachaService WithOptions(CallOptions option)
        {
            return this;
        }

        public IGachaService WithHeaders(Metadata headers)
        {
            LastHeaders = headers;
            return this;
        }

        public IGachaService WithDeadline(DateTime deadline)
        {
            return this;
        }

        public IGachaService WithCancellationToken(CancellationToken cancellationToken)
        {
            return this;
        }

        public IGachaService WithHost(string host)
        {
            throw new NotSupportedException();
        }

        private static Func<UnaryResult<T>> Next<T>(Queue<Func<UnaryResult<T>>> queue)
        {
            if (queue.Count == 0)
            {
                throw new InvalidOperationException("FakeGachaService: no result");
            }

            return queue.Dequeue();
        }
    }
}
