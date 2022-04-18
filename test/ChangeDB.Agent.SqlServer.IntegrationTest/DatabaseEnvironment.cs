using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TestDB;
using TestDB.SqlServer;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IAsyncDisposable, IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        public const string DbType = "sqlserver";

        public DatabaseEnvironment()
        {
            Databases.SetupDatabase<SqlServerInstance, SqlServerProvider>(DbType);
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
