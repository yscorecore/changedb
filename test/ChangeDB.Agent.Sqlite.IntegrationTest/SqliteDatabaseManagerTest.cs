using System;
using System.IO;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.Sqlite
{
    public class SqlServerDatabaseManagerTest : BaseTest
    {
        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {
            await using var database = CreateDatabase(false);
            var databaseManager = SqliteDatabaseManager.Default;
            await databaseManager.DropDatabaseIfExists(database.ConnectionString);
            File.Exists(database.DatabaseName).Should().BeFalse();
        }

        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await using var database = RequestDatabase();
            var databaseManager = SqliteDatabaseManager.Default;
            await databaseManager.CreateDatabase(database.ConnectionString);
            Action action = () =>
            {
                database.Connection.Open();
            };
            action.Should().NotThrow();
        }
    }
}
