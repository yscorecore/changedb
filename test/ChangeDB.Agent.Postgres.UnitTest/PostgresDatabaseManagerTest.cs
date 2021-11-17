using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDatabaseManagerTest
    {
        private readonly IDatabaseManager _databaseManager = PostgresDatabaseManager.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting();
        private readonly DbConnection _dbConnection;

        public PostgresDatabaseManagerTest()
        {
            _dbConnection = new NpgsqlConnection($"Server=127.0.0.1;Port=5432;Database={TestUtils.RandomDatabaseName()};User Id=postgres;Password=mypassword;");
            _dbConnection.CreateDatabase();
        }
        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {
            await _databaseManager.DropDatabaseIfExists(_dbConnection, _migrationSetting);
            Action action = () =>
            {
                _dbConnection.Open();
            };
            action.Should().Throw<Npgsql.PostgresException>()
                .WithMessage("3D000: database \"*\" does not exist");

        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await _databaseManager.DropDatabaseIfExists(_dbConnection, _migrationSetting);
            await _databaseManager.CreateDatabase(_dbConnection, _migrationSetting);
            var currentDatabase = _dbConnection.ExecuteScalar<string>("select current_database()");
            currentDatabase.Should().NotBeEmpty();
        }
    }
}
