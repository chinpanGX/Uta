using MagicOnion.Client;
using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Support;
using VolTech.Server.Abstractions;

namespace Uta.Client.Infrastructure.Api
{
    [MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService), typeof(IGachaService), typeof(ISupportService))]
    internal partial class MagicOnionClientInitializer
    {
    }
}
