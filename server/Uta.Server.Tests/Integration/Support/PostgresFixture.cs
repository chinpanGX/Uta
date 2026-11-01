using Npgsql;
using Testcontainers.PostgreSql;
using Uta.Server.Infrastructure;

namespace Uta.Server.Tests.Integration.Support;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        UtaDatabase.Migrate(ConnectionString);
        // 2 回目は何もしない
        UtaDatabase.Migrate(ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }

    /// <summary>1 行 1 列を読む。無い（または NULL の）ときは default。</summary>
    public async Task<T?> QueryScalarAsync<T>(string sql, params object[] parameters)
    {
        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter);
        }

        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)value;
    }

    /// <summary>結果を返さない SQL（INSERT など）を流す。</summary>
    public async Task ExecuteAsync(string sql, params object[] parameters)
    {
        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter);
        }

        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
