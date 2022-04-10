using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.MySql.UnitTest
{
    [Collection(nameof(DatabaseEnvironment))]
    public class MySqlDatabaseManagerTest
    {
        private readonly IDatabaseManager _databaseManager = MySqlDatabaseManager.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;

        public MySqlDatabaseManagerTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.NewDatabaseConnection();
            _migrationContext = new MigrationContext
            {
                TargetConnection = _dbConnection,
            };

            _dbConnection.CreateDatabase();
        }
        [Fact]
        [Obsolete]
        public async Task ShouldDropCurrentDatabase()
        {
            //await _databaseManager.DropTargetDatabaseIfExists(_migrationContext);
            //Action action = () =>
            //{
            //    _dbConnection.Open();
            //};
            //action.Should().Throw<Exception>()
            //    .WithMessage("Unknown database '*'");

        }
        [Fact]
        [Obsolete]
        public async Task ShouldCreateNewDatabase()
        {
            //await _databaseManager.DropTargetDatabaseIfExists(_migrationContext);
            //await _databaseManager.CreateTargetDatabase(_migrationContext);
            //var currentDatabase = _dbConnection.ExecuteScalar<string>("select database()");
            //currentDatabase.Should().NotBeEmpty();
        }
    }
}
