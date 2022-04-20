using System;
using System.Collections.Generic;
using System.Data.Common;
using Npgsql;
using TestDB;
using TestDB.Postgres;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        public const string DbType = "postgres";

        public DatabaseEnvironment()
        {
            Databases.SetupDatabase<PostgresInstance, PostgresProvider>(DbType);
        }

        public void Dispose()
        {
            Databases.DisposeAll();
        }

    }
}
