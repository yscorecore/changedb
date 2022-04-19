using System;
using System.Data.SqlServerCe;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{

    public class SqlCeDatabaseManagerTest : BaseTest
    {
        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {
            Func<Task> action = async () =>
            {
                await using var database = CreateDatabase(false);
                IDatabaseManager _databaseManager = SqlCeDatabaseManager.Default;
                await _databaseManager.DropTargetDatabaseIfExists(database.ConnectionString, new MigrationSetting());
                database.Connection.Open();
            };
            await action.Should().ThrowAsync<SqlCeException>()
                 .WithMessage("The database file cannot be found. *");
        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await using var database = RequestDatabase();
            IDatabaseManager _databaseManager = SqlCeDatabaseManager.Default;
            await _databaseManager.CreateDatabase(database.ConnectionString, new MigrationSetting());
            Action action = () =>
            {
                database.Connection.Open();
            };
            action.Should().NotThrow();
        }
    }
}
