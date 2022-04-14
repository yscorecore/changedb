using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlServerDatabaseManagerTest
    {
        private readonly IDatabaseManager _databaseManager = SqlServerDatabaseManager.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;

        public SqlServerDatabaseManagerTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.NewDatabaseConnection();
            _migrationContext = new MigrationContext
            {
                TargetConnection = _dbConnection
            };
            _dbConnection.CreateDatabase();
        }
        [Fact]
        [Obsolete]
        public Task ShouldDropCurrentDatabase()
        {
            return Task.CompletedTask;
        }

        [Fact]
        [Obsolete]
        public Task ShouldCreateNewDatabase()
        {
            return Task.CompletedTask;
        }
    }
}
