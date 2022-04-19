using System;
using System.Data.Common;
using System.Data.SqlServerCe;
using TestDB;
using TestDB.SqlCe;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        public const string DbType = "sqlce";
        public DatabaseEnvironment()
        {
            Databases.SetupDatabase<SqlCeInstance, SqlCeProvider>(DbType, true);
        }

        public void Dispose()
        {
            Databases.DisposeAll();
        }

    }
}
