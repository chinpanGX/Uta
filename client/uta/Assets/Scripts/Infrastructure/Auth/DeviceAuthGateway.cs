using System;
using Cysharp.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using VolTech.Client;
using VolTech.Client.Auth;
using VolTech.Client.Unity;
using VolTech.Server.Abstractions;
using Uta.Client.Application.Auth;
using Uta.Client.Infrastructure.Api;

namespace Uta.Client.Infrastructure.Auth
{
    public sealed class DeviceAuthGateway : IDeviceAuthPort, IDisposable
    {
        private readonly GrpcChannel channel;
        private readonly IDeviceService client;
        private readonly DeviceTokenProvider provider;

        private DeviceAuthGateway(GrpcChannel channel, IDeviceService client, DeviceTokenProvider provider)
        {
            this.channel = channel;
            this.client = client;
            this.provider = provider;
        }

        /// <param name="serverUrl">例: http://localhost:5000</param>
        /// <param name="platform">AppVersion.Windows など（Bootstrap が決める）</param>
        public static DeviceAuthGateway Create(string serverUrl, string platform, int appVersion, string accountFilePath, string keySource)
        {
            var channel = ApiChannel.Create(serverUrl);
            var callInvoker = ApiChannel.CreateCallInvoker(channel);
            var client = DeviceServiceClientFactory.Create(callInvoker);
            var provider = new DeviceTokenProvider(
                client,
                new EncryptedFileAccountStore(accountFilePath, keySource),
                new ApiCoreOptions(new AppVersion(platform, appVersion)));
            return new DeviceAuthGateway(channel, client, provider);
        }

        public async UniTask<AccountState> GetAccountStateAsync()
        {
            var status = await provider.GetAccountStatusAsync();
            switch (status)
            {
                case AccountStatus.NotRegistered:
                    return AccountState.NotRegistered;
                case AccountStatus.Registered:
                    return AccountState.Registered;
                case AccountStatus.Corrupted:
                    return AccountState.Corrupted;
                default:
                    throw new InvalidOperationException("unknown account status: " + status);
            }
        }

        public async UniTask EnsureSignedInAsync()
        {
            // トークンは使わずに捨てる
            await provider.GetTokenAsync();
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

        public void Dispose()
        {
            channel.Dispose();
        }
    }
}
