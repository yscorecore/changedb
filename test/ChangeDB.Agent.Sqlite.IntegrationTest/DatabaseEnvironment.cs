using System;
using System.Threading.Tasks;
using TestDB;
using TestDB.Sqlite;
using Xunit;

namespace ChangeDB.Agent.Sqlite
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IAsyncDisposable, IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        public const string DbType = "sqlite";

        public DatabaseEnvironment()
        {
            Databases.SetupDatabase<SqliteInstance, SqliteProvider>(DbType, false);
        }
        public void Dispose()
        {
            Databases.DisposeAll();
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }
    }
}
