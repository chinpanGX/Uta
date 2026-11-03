using MagicOnion.Client;
using Uta.Shared.Contracts.Player;
using VolTech.Server.Abstractions;

namespace Uta.Client.Infrastructure.Api
{
    [MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService))]
    internal partial class UtaMagicOnionClientInitializer
    {
    }
}
