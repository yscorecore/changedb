using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;


namespace ChangeDB.Agent.Postgres
{
    [Collection(nameof(DatabaseEnvironment))]
    public class PostgresDatabaseManagerTest
    {
        private readonly IDatabaseManager _databaseManager = PostgresDatabaseManager.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;

        public PostgresDatabaseManagerTest(DatabaseEnvironment databaseEnvironment)
        {
            //_dbConnection = databaseEnvironment.NewDatabaseConnection();
            //_migrationContext = new MigrationContext
            //{
            //    TargetConnection = _dbConnection
            //};

            //_dbConnection.CreateDatabase();
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
