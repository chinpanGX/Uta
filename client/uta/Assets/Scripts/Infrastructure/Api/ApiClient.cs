using System;
using Grpc.Net.Client;
using Uta.Shared.Contracts.Common;
using Uta.Shared.Contracts.Player;
using VolTech.Client;
using VolTech.Client.Auth;
using VolTech.Client.Unity;
using VolTech.Server.Abstractions;

namespace Uta.Client.Infrastructure.Api
{
    public sealed class ApiClient : IDisposable
    {
        private readonly GrpcChannel channel;
        private readonly ApiCoreOptions options;
        private readonly string accountFilePath;
        private readonly string keySource;
        private readonly CountingTokenProvider counting;

        public IDeviceService DeviceClient { get; }

        /// <summary>ApiCore が使うトークンの提供者。</summary>
        public DeviceTokenProvider TokenProvider { get; }

        public IPlayerService Player { get; }

        /// <summary>ApiCore がトークンを捨てた（Invalidate を呼んだ）回数。再認証の再送の診断用。</summary>
        public int TokenInvalidationCount => counting.InvalidationCount;

        private ApiClient(GrpcChannel channel, IDeviceService deviceClient, ApiCoreOptions options, string accountFilePath, string keySource, DeviceTokenProvider tokenProvider, CountingTokenProvider counting, IPlayerService player)
        {
            this.counting = counting;
            this.channel = channel;
            this.options = options;
            this.accountFilePath = accountFilePath;
            this.keySource = keySource;
            DeviceClient = deviceClient;
            TokenProvider = tokenProvider;
            Player = player;
        }

        /// <param name="errorHandler">アプリの共通エラーハンドラー（Bootstrap が DefaultCommonErrorHandler を渡す）</param>
        /// <param name="errorNavigator">エラー後の画面遷移。null なら付けない（診断のとき）</param>
        /// <param name="commonApplier">Common の反映先。null なら付けない（診断のとき）</param>
        public static ApiClient Create(string serverUrl, string platform, int appVersion, string accountFilePath, string keySource, ICommonErrorHandler errorHandler, IErrorNavigator errorNavigator, ICommonResponseApplier<AppCommonDto> commonApplier)
        {
            var channel = ApiChannel.Create(serverUrl);
            var callInvoker = ApiChannel.CreateCallInvoker(channel);
            var options = new ApiCoreOptions(new AppVersion(platform, appVersion));
            var deviceClient = ServiceClientFactory.Create<IDeviceService>(callInvoker);
            var tokenProvider = new DeviceTokenProvider(deviceClient, new EncryptedFileAccountStore(accountFilePath, keySource), options);
            var counting = new CountingTokenProvider(tokenProvider);
            var builder = ApiCore.CreateBuilder(
                    callInvoker, ServiceClientFactory.SerializerProvider, ServiceClientFactory.ClientFactoryProvider,
                    options, counting, new ErrorPolicyProvider(ErrorDefinitions.Create()), errorHandler)
                .WithErrorNavigator(errorNavigator);
            if (commonApplier != null)
            {
                builder = builder.WithCommonResponseApplier(commonApplier);
            }

            var core = builder.Build();
            var playerClient = core.CreateClient<IPlayerService>();
            return new ApiClient(channel, deviceClient, options, accountFilePath, keySource, tokenProvider, counting, playerClient);
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
