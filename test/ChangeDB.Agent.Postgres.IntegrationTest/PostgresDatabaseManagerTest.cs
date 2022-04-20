using System;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;


namespace ChangeDB.Agent.Postgres
{
    public class PostgresDatabaseManagerTest : BaseTest
    {

        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {

            Func<Task> action = async () =>
            {
                await using var database = CreateDatabase(false);
                var databaseManager = PostgresDatabaseManager.Default;
                await databaseManager.DropTargetDatabaseIfExists(database.ConnectionString, new MigrationSetting());
                database.Connection.Open();
            };
            await action.Should().ThrowAsync<PostgresException>()
                .WithMessage("3D000: database \"*\" does not exist");
        }

        [Fact]
        public async Task ShouldCreateNewDatabase()
        {

            await using var database = RequestDatabase();
            var databaseManager = PostgresDatabaseManager.Default;
            await databaseManager.CreateDatabase(database.ConnectionString, new MigrationSetting());
            var currentDatabase = database.Connection.ExecuteScalar<string>("select current_database()");
            currentDatabase.Should().Be(database.DatabaseName);
        }
    }
}
