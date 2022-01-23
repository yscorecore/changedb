using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FirebirdSql.Data.FirebirdClient;
using FluentAssertions;
using Xunit;


namespace ChangeDB.Agent.Firebird
{
    [Collection(nameof(DatabaseEnvironment))]
    public class FirebirdDatabaseManagerTest
    {
        private readonly IDatabaseManager _databaseManager = FirebirdDatabaseManager.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;

        public FirebirdDatabaseManagerTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.NewDatabaseConnection();
            _migrationContext = new MigrationContext
            {
                TargetConnection = _dbConnection
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
            action.Should().Throw<FbException>()
                .WithMessage("*Error while trying to open file");

        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await _databaseManager.DropTargetDatabaseIfExists(_migrationContext);
            await _databaseManager.CreateTargetDatabase(_migrationContext);
            var currentDatabase = _dbConnection.ExecuteScalar<string>("SELECT rdb$get_context('SYSTEM', 'DB_NAME') FROM RDB$DATABASE");
            currentDatabase.Should().NotBeEmpty();
        }
    }
}
