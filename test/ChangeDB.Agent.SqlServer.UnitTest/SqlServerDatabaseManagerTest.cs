using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDatabaseManagerTest
    {
        private readonly IDatabaseManager _databaseManager = SqlServerDatabaseManager.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting();
        private readonly DbConnection _dbConnection;

        public SqlServerDatabaseManagerTest()
        {
            _dbConnection = new SqlConnection($"Server=127.0.0.1,1433;Database={TestUtils.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;");
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
            action.Should().Throw<SqlException>()
                .WithMessage("Cannot open database \"*\" *");

        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await _databaseManager.DropDatabaseIfExists(_dbConnection, _migrationSetting);
            await _databaseManager.CreateDatabase(_dbConnection, _migrationSetting);
            var currentDatabase = _dbConnection.ExecuteScalar<string>("select DB_NAME()");
            currentDatabase.Should().NotBeEmpty();
        }
    }
}
