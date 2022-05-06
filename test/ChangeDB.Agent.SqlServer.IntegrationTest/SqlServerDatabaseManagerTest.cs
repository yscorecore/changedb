using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDatabaseManagerTest : BaseTest
    {

        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {
            Func<Task> action = async () =>
            {
                await using var database = CreateDatabase(false);
                var databaseManager = SqlServerDatabaseManager.Default;
                await databaseManager.DropDatabaseIfExists(database.ConnectionString);
                database.Connection.Open();
            };
            await action.Should().ThrowAsync<Exception>()
                .WithMessage("Cannot open database \"*\" *");
        }

        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await using var database = RequestDatabase();
            var databaseManager = SqlServerDatabaseManager.Default;
            await databaseManager.CreateDatabase(database.ConnectionString);
            var currentDatabase = database.Connection.ExecuteScalar<string>("select DB_NAME()");
            currentDatabase.Should().Be(database.DatabaseName);
        }
    }
}
