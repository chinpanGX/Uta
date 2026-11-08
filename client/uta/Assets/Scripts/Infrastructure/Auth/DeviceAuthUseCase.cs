using System;
using Cysharp.Threading.Tasks;
using Grpc.Core;
using VolTech.Client;
using VolTech.Client.Auth;
using VolTech.Server.Abstractions;
using Uta.Client.Application.Auth;
using Uta.Client.Infrastructure.Api;

namespace Uta.Client.Infrastructure.Auth
{
    public sealed class DeviceAuthUseCase : IDeviceAuthUseCase
    {
        private readonly ApiClient api;
        private readonly IDeviceService client;
        private readonly DeviceTokenProvider provider;

        public DeviceAuthUseCase(ApiClient api)
        {
            this.api = api;
            client = api.DeviceClient;
            provider = api.TokenProvider;
        }

        public async UniTask<bool> SignInAsync()
        {
            // 状態は先に 1 回だけ取る（トークンを取った後に取ると、常に Registered になる）
            var status = await provider.GetAccountStatusAsync();
            switch (status)
            {
                case AccountStatus.NotRegistered:
                case AccountStatus.Registered:
                    break;
                case AccountStatus.Corrupted:
                    throw new InvalidOperationException("account corrupted");
                default:
                    throw new InvalidOperationException("unknown account status: " + status);
            }

            // トークンは使わずに捨てる
            await provider.GetTokenAsync();
            return status == AccountStatus.NotRegistered;
        }

        public async UniTask ReissueTokenAsync()
        {
            provider.Invalidate(null);
            await provider.GetTokenAsync();
        }

        public async UniTask<bool> CanReceiveServerErrorAsync()
        {
            // 締め切りを付けないと、サーバーを止めた実行が長くぶら下がる
            var withDeadline = client.WithOptions(new CallOptions(deadline: DateTime.UtcNow.AddSeconds(15)));
            try
            {
                await withDeadline.IssueTokenAsync("00000000000000000000000000", "0123456789abcdef");
                return false;
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Unauthenticated)
            {
                return true;
            }
        }

        public int TokenInvalidationCount => api.TokenInvalidationCount;

        public async UniTask InvalidateServerSideTokenAsync()
        {
            var other = api.CreateTokenProvider();
            await other.GetAccountStatusAsync();
            other.Invalidate(null);
            await other.GetTokenAsync();   // サーバーが新しいトークンを発行し、古いトークン（メインの持つもの）が無効になる
        }
    }
}
