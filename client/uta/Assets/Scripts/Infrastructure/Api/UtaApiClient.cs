using System;
using Grpc.Net.Client;
using Uta.Shared.Contracts.Player;
using VolTech.Client;
using VolTech.Client.Auth;
using VolTech.Client.Unity;
using VolTech.Server.Abstractions;

namespace Uta.Client.Infrastructure.Api
{
    public sealed class UtaApiClient : IDisposable
    {
        private readonly GrpcChannel channel;
        private readonly ApiCoreOptions options;
        private readonly string accountFilePath;
        private readonly string keySource;

        public IDeviceService DeviceClient { get; }

        /// <summary>ApiCore が使うトークンの提供者。</summary>
        public DeviceTokenProvider TokenProvider { get; }

        public PlayerApi Player { get; }

        private UtaApiClient(GrpcChannel channel, IDeviceService deviceClient, ApiCoreOptions options, string accountFilePath, string keySource, DeviceTokenProvider tokenProvider, PlayerApi player)
        {
            this.channel = channel;
            this.options = options;
            this.accountFilePath = accountFilePath;
            this.keySource = keySource;
            DeviceClient = deviceClient;
            TokenProvider = tokenProvider;
            Player = player;
        }

        /// <param name="errorHandler">アプリの共通エラーハンドラー（Bootstrap が DefaultCommonErrorHandler を渡す）</param>
        public static UtaApiClient Create(string serverUrl, string platform, int appVersion, string accountFilePath, string keySource, ICommonErrorHandler errorHandler)
        {
            var channel = ApiChannel.Create(serverUrl);
            var callInvoker = ApiChannel.CreateCallInvoker(channel);
            var options = new ApiCoreOptions(new AppVersion(platform, appVersion));
            var deviceClient = ServiceClientFactory.Create<IDeviceService>(callInvoker);
            var playerClient = ServiceClientFactory.Create<IPlayerService>(callInvoker);
            var tokenProvider = new DeviceTokenProvider(deviceClient, new EncryptedFileAccountStore(accountFilePath, keySource), options);
            var core = ApiCore.CreateBuilder(options, tokenProvider, new ErrorPolicyProvider(UtaErrors.Create()), errorHandler).Build();
            return new UtaApiClient(channel, deviceClient, options, accountFilePath, keySource, tokenProvider, new PlayerApi(core, playerClient));
        }

        /// <summary>同じ端末のアカウントのファイルを読む、もう 1 つの DeviceTokenProvider（診断で、メインの持つトークンをサーバーで無効にするために使う）。</summary>
        public DeviceTokenProvider CreateTokenProvider()
        {
            return new DeviceTokenProvider(DeviceClient, new EncryptedFileAccountStore(accountFilePath, keySource), options);
        }

        public void Dispose()
        {
            channel.Dispose();
        }
    }
}
