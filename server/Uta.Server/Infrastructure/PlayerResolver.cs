using Dapper;
using Npgsql;
using VolTech.Server.Auth;

namespace Uta.Server.Infrastructure;

public sealed class PlayerResolver(NpgsqlDataSource dataSource) : IPlayerResolver
{
    public async ValueTask<PlayerStatus> ResolveAsync(Ulid playerId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var exists = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT 1 FROM players WHERE player_id = @playerId",
            new { playerId = playerId.ToString() },
            cancellationToken: cancellationToken));
        return exists is null ? PlayerStatus.NotCreated : PlayerStatus.Active;
    }
}
