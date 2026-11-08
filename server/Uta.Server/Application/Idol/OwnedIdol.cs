namespace Uta.Server.Application.Idol;

public sealed record OwnedIdol(Ulid Uid, int IdolId, int Training, int Awakening, int Shards);
