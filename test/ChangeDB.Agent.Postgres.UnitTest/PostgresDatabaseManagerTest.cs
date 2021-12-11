﻿using System;
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
        private readonly MigrationContext _migrationContext = new MigrationContext();
        private readonly DbConnection _dbConnection;

        public PostgresDatabaseManagerTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.NewDatabaseConnection();
            _dbConnection.CreateDatabase();
        }
        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {
            await _databaseManager.DropDatabaseIfExists(_dbConnection, _migrationContext);
            Action action = () =>
            {
                _dbConnection.Open();
            };
            action.Should().Throw<PostgresException>()
                .WithMessage("3D000: database \"*\" does not exist");

        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await _databaseManager.DropDatabaseIfExists(_dbConnection, _migrationContext);
            await _databaseManager.CreateDatabase(_dbConnection, _migrationContext);
            var currentDatabase = _dbConnection.ExecuteScalar<string>("select current_database()");
            currentDatabase.Should().NotBeEmpty();
        }
    }
}
