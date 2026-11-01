using Uta.Shared.Domain.Rng;

namespace Uta.Server.Application.Common;

public interface IRandomSource
{
    /// <summary>要求ごとに 1 回呼ぶ（シードは保存しない）。</summary>
    IRandom Create();
}
