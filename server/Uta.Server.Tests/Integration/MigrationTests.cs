using Uta.Server.Tests.Integration.Support;

namespace Uta.Server.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class MigrationTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Migration_CreatesTables()
    {
        var count = await postgres.QueryScalarAsync<long>(
            "SELECT count(*) FROM information_schema.tables WHERE table_name IN ('produce_sessions','produce_results')");

        Assert.Equal(2, count);
    }
}
