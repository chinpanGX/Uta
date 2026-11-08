namespace Uta.Server.Domain.Support;

public sealed record SupportCardEntity(Ulid UniqueId, int MasterId, int Level, int LimitBreak);
