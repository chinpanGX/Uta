using Uta.Server.Domain.Idol;
using Uta.Server.Domain.Player;
using Uta.Server.Domain.Support;

namespace Uta.Server.Application.Player;

public sealed record SignInResult(PlayerEntity Player, IReadOnlyList<IdolEntity> Idols, IReadOnlyList<SupportCardEntity> SupportCards, bool HasActiveProduce, bool DiscardedProduce);
