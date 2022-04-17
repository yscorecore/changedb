using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestDB;
using TestDB.MySql;
using TestDB.Postgres;
using TestDB.SqlCe;
using TestDB.SqlServer;
using Xunit;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        internal static readonly string[] SupportedDatabases = new[] { "sqlserver", "sqlce", "postgres", "mysql" };
        public DatabaseEnvironment()
        {
            Databases.SetupDatabase<SqlServerInstance, SqlServerProvider>("sqlserver");
            Databases.SetupDatabase<SqlCeInstance, SqlCeProvider>("sqlce");
            Databases.SetupDatabase<PostgresInstance, PostgresProvider>("postgres");
            Databases.SetupDatabase<MySqlInstance, MySqlProvider>("mysql");
        }
        public void Dispose()
        {
            Databases.DisposeAll();
        }
    }
}
