using System;
using System.Collections.Generic;
using System.Data.Common;
using MySqlConnector;
using TestDB;
using TestDB.MySql;
using Xunit;

namespace ChangeDB.Agent.MySql
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        public const string DbType = "mysql";

        public DatabaseEnvironment()
        {
            Databases.SetupDatabase<MySqlInstance, MySqlProvider>(DbType);
        }

        public void Dispose()
        {
            Databases.DisposeAll();
        }
    }
}
