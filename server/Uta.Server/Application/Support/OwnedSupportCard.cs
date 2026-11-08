namespace Uta.Server.Application.Support;

public sealed record OwnedSupportCard(Ulid UniqueId, int MasterId, int Level, int LimitBreak);
