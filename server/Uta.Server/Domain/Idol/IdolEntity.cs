namespace Uta.Server.Domain.Idol;

public sealed record IdolEntity(Ulid UniqueId, int MasterId, int Training, int Awakening, int Shards);
