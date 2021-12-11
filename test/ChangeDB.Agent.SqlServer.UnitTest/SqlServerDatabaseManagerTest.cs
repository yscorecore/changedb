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
                Target = new AgentRunTimeInfo { Connection = _dbConnection }
            };
            _dbConnection.CreateDatabase();
        }
        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {
            await _databaseManager.DropTargetDatabaseIfExists(_migrationContext);
            Action action = () =>
            {
                _dbConnection.Open();
            };
            action.Should().Throw<SqlException>()
                .WithMessage("Cannot open database \"*\" *");

        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await _databaseManager.DropTargetDatabaseIfExists(_migrationContext);
            await _databaseManager.CreateTargetDatabase(_migrationContext);
            var currentDatabase = _dbConnection.ExecuteScalar<string>("select DB_NAME()");
            currentDatabase.Should().NotBeEmpty();
        }
    }
}
