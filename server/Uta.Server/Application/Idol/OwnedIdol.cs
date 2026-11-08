namespace Uta.Server.Application.Idol;

public sealed record OwnedIdol(Ulid UniqueId, int MasterId, int Training, int Awakening, int Shards);
