using System;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDatabaseManagerTest
    {
        private readonly IDatabaseManager _databaseManager = new SqlCeAgent().DatabaseManger;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;

        public SqlCeDatabaseManagerTest()
        {
            _dbConnection = new SqlCeConnection($"Data Source={Utility.RandomDatabaseName()}.sdf;Persist Security Info=False;");
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
