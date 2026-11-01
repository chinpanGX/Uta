using Uta.Server.Application.Common;
using Uta.Server.Domain.Gacha;
using Uta.Shared.Domain.Rng;

namespace Uta.Server.Infrastructure;

public sealed class SecureRandomSource : IRandomSource
{
    public IRandom Create() => GachaRandom.CreateSecure();
}
