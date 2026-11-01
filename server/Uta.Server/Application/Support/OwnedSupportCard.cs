namespace Uta.Server.Application.Support;

public sealed record OwnedSupportCard(Ulid Uid, int CardId, int Level, int LimitBreak);
