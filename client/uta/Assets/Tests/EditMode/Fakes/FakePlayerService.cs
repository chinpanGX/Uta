using System;
using System.Collections.Generic;
using System.Threading;
using Grpc.Core;
using MagicOnion;
using Uta.Shared.Contracts.Player;
using VolTech.Client;
using VolTech.Server.Abstractions;

namespace Uta.Client.Tests
{
    /// <summary>
    /// 通信する代わりに、積んだ結果を返す IPlayerService。呼び出しは Calls に記録する。
    /// WithHandledCodes（WithHeaders）は自分を返す。ApiCore を通らないので、処理済みのコードの扱いは確かめない。
    /// </summary>
    public sealed class FakePlayerService : IPlayerService
    {
        public readonly Queue<Func<UnaryResult<SignUpResponse>>> SignUpResults = new Queue<Func<UnaryResult<SignUpResponse>>>();
        public readonly Queue<Func<UnaryResult<SignInResponse>>> SignInResults = new Queue<Func<UnaryResult<SignInResponse>>>();
        public readonly List<string> Calls = new List<string>();

        public static Func<UnaryResult<T>> Ok<T>(T value)
        {
            return () => new UnaryResult<T>(value);
        }

        public static Func<UnaryResult<T>> Fail<T>(ErrorCode code)
        {
            return () => FakeRpcErrors.ApiFailureResult<T>(code);
        }

        public UnaryResult<SignUpResponse> SignUpAsync()
        {
            Calls.Add("SignUp");
            return Next(SignUpResults)();
        }

        public UnaryResult<SignInResponse> SignInAsync()
        {
            Calls.Add("SignIn");
            return Next(SignInResults)();
        }

        public IPlayerService WithOptions(CallOptions option)
        {
            return this;
        }

        public IPlayerService WithHeaders(Metadata headers)
        {
            return this;
        }

        public IPlayerService WithDeadline(DateTime deadline)
        {
            return this;
        }

        public IPlayerService WithCancellationToken(CancellationToken cancellationToken)
        {
            return this;
        }

        public IPlayerService WithHost(string host)
        {
            throw new NotSupportedException();
        }

        private static Func<UnaryResult<T>> Next<T>(Queue<Func<UnaryResult<T>>> queue)
        {
            if (queue.Count == 0)
            {
                throw new InvalidOperationException("FakePlayerService: no result");
            }

            return queue.Dequeue();
        }
    }
}
