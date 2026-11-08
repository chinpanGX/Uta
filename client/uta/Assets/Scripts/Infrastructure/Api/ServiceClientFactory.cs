using System;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Serialization;
using MagicOnion.Serialization.MessagePack;
using MessagePack;
using MessagePack.Resolvers;

namespace Uta.Client.Infrastructure.Api
{
    internal static class ServiceClientFactory
    {
        public static IMagicOnionSerializerProvider SerializerProvider { get; } = CreateSerializerProvider();

        public static IMagicOnionClientFactoryProvider ClientFactoryProvider => MagicOnionClientInitializer.ClientFactoryProvider;

        /// <summary>IDeviceService 用。DeviceTokenProvider は ApiCore の前に要るので、ApiCore を通さずに作る。</summary>
        public static TService Create<TService>(CallInvoker callInvoker)
            where TService : IService<TService>
        {
            return MagicOnionClient.Create<TService>(callInvoker, SerializerProvider, Array.Empty<IClientFilter>(), ClientFactoryProvider);
        }

        private static IMagicOnionSerializerProvider CreateSerializerProvider()
        {
            var options = MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(MagicOnionClientInitializer.Resolver, StandardResolver.Instance));
            return MessagePackMagicOnionSerializerProvider.Default.WithOptions(options);
        }
    }
}
