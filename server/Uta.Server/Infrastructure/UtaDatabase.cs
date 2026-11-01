using DbUp;
using VolTech.Server.Transaction;

namespace Uta.Server.Infrastructure;

public static class UtaDatabase
{
    /// <summary>VolTech-MagicOnion のテーブル、Uta のテーブルの順に流す。失敗したら InvalidOperationException。</summary>
    public static void Migrate(string connectionString)
    {
        VoltechDatabase.Migrate(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(UtaDatabase).Assembly,
                name => name.StartsWith("Uta.Server.Migrations.", StringComparison.Ordinal))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException($"Uta のマイグレーション {result.ErrorScript?.Name} に失敗した。", result.Error);
        }
    }
}
