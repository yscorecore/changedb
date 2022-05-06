using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using MySqlConnector;
using Xunit;

namespace ChangeDB.Agent.MySql
{
    public class MySqlDatabaseManagerTest : BaseTest
    {

        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {
            Func<Task> action = async () =>
            {
                await using var database = CreateDatabase(false);
                var databaseManager = MySqlDatabaseManager.Default;
                await databaseManager.DropDatabaseIfExists(database.ConnectionString);
                database.Connection.Open();
            };
            await action.Should().ThrowAsync<MySqlException>()
                .WithMessage("Unknown database '*'");
        }

        [Fact]
        public async Task ShouldCreateNewDatabase()
        {

            await using var database = RequestDatabase();
            var databaseManager = MySqlDatabaseManager.Default;
            await databaseManager.CreateDatabase(database.ConnectionString);
            var currentDatabase = database.Connection.ExecuteScalar<string>("select database()");
            currentDatabase.Should().Be(database.DatabaseName);
        }
    }
}
