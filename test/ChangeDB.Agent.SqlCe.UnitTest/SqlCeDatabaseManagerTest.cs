using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDatabaseManagerTest
    {
        private readonly IDatabaseManager _databaseManager = new SqlCeMigrationAgent().DatabaseManger;
        private readonly MigrationContext _migrationContext = new MigrationContext();
        private readonly DbConnection _dbConnection;

        public SqlCeDatabaseManagerTest()
        {
            _dbConnection = new SqlCeConnection($"Data Source={TestUtils.RandomDatabaseName()}.sdf;Persist Security Info=False;");
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
            action.Should().Throw<SqlCeException>()
                .WithMessage("The database file cannot be found. *");

        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await _databaseManager.DropDatabaseIfExists(_dbConnection, _migrationContext);
            await _databaseManager.CreateDatabase(_dbConnection, _migrationContext);
            Action action = () =>
            {
                _dbConnection.Open();
            };
            action.Should().NotThrow();
        }
    }
}
