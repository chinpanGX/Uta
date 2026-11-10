using System;
using System.Collections.Generic;
using System.Threading;
using Grpc.Core;
using MagicOnion;
using Uta.Shared.Contracts.Support;
using VolTech.Client;
using VolTech.Server.Abstractions;

namespace Uta.Client.Tests
{
    /// <summary>通信する代わりに、積んだ結果を返す ISupportService。呼び出しは Calls に記録する。</summary>
    public sealed class FakeSupportService : ISupportService
    {
        public readonly Queue<Func<UnaryResult<LevelUpResponse>>> LevelUpResults = new Queue<Func<UnaryResult<LevelUpResponse>>>();
        public readonly Queue<Func<UnaryResult<LimitBreakResponse>>> LimitBreakResults = new Queue<Func<UnaryResult<LimitBreakResponse>>>();
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

        public UnaryResult<LevelUpResponse> LevelUpAsync(LevelUpRequest request)
        {
            Calls.Add("LevelUp:" + request.SupportCardUniqueId + "," + request.TargetLevel);
            return Next(LevelUpResults)();
        }

        public UnaryResult<LimitBreakResponse> LimitBreakAsync(LimitBreakRequest request)
        {
            Calls.Add("LimitBreak:" + request.SupportCardUniqueId + "," + request.MaterialUniqueId);
            return Next(LimitBreakResults)();
        }

        public ISupportService WithOptions(CallOptions option)
        {
            return this;
        }

        public ISupportService WithHeaders(Metadata headers)
        {
            LastHeaders = headers;
            return this;
        }

        public ISupportService WithDeadline(DateTime deadline)
        {
            return this;
        }

        public ISupportService WithCancellationToken(CancellationToken cancellationToken)
        {
            return this;
        }

        public ISupportService WithHost(string host)
        {
            throw new NotSupportedException();
        }

        private static Func<UnaryResult<T>> Next<T>(Queue<Func<UnaryResult<T>>> queue)
        {
            if (queue.Count == 0)
            {
                throw new InvalidOperationException("FakeSupportService: no result");
            }

            return queue.Dequeue();
        }
    }
}
