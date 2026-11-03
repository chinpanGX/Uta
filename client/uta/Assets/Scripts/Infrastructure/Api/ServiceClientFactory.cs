using System;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Serialization.MessagePack;
using MessagePack;
using MessagePack.Resolvers;

namespace Uta.Client.Infrastructure.Api
{
    internal static class ServiceClientFactory
    {
        public static TService Create<TService>(CallInvoker callInvoker)
            where TService : IService<TService>
        {
            var options = MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(UtaMagicOnionClientInitializer.Resolver, StandardResolver.Instance));
            var serializerProvider = MessagePackMagicOnionSerializerProvider.Default.WithOptions(options);

            return MagicOnionClient.Create<TService>(
                callInvoker, serializerProvider, Array.Empty<IClientFilter>(), UtaMagicOnionClientInitializer.ClientFactoryProvider);
        }
    }
}
