using System;
using Grpc.Core;
using MagicOnion.Client;
using MagicOnion.Serialization.MessagePack;
using MessagePack;
using MessagePack.Resolvers;
using VolTech.Server.Abstractions;

namespace Uta.Client.Infrastructure.Api
{
    internal static class DeviceServiceClientFactory
    {
        public static IDeviceService Create(CallInvoker callInvoker)
        {
            var options = MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(UtaMagicOnionClientInitializer.Resolver, StandardResolver.Instance));
            var serializerProvider = MessagePackMagicOnionSerializerProvider.Default.WithOptions(options);

            return MagicOnionClient.Create<IDeviceService>(
                callInvoker, serializerProvider, Array.Empty<IClientFilter>(), UtaMagicOnionClientInitializer.ClientFactoryProvider);
        }
    }
}
