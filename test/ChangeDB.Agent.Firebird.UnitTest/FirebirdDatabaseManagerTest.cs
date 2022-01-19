using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
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
            action.Should().Throw<Exception>()
                .WithMessage("3D000: database \"*\" does not exist");

        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await _databaseManager.DropTargetDatabaseIfExists(_migrationContext);
            await _databaseManager.CreateTargetDatabase(_migrationContext);
            var currentDatabase = _dbConnection.ExecuteScalar<string>("select current_database()");
            currentDatabase.Should().NotBeEmpty();
        }
    }
}
