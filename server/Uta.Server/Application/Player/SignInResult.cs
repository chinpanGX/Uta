using Uta.Server.Application.Idol;
using Uta.Server.Application.Support;

namespace Uta.Server.Application.Player;

public sealed record SignInResult(PlayerSnapshot Player, IReadOnlyList<OwnedIdol> Idols, IReadOnlyList<OwnedSupportCard> SupportCards);
