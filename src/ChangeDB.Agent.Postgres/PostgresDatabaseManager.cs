using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.Postgres.PostgresUtils;
namespace ChangeDB.Agent.Postgres
{
    public class PostgresDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new PostgresDatabaseManager();


        public Task CreateDatabase(string connectionString, MigrationSetting migrationSetting)
        {
            ConnectionExtensions.CreateDatabase(connectionString);
            return Task.CompletedTask;
        }

        public Task DropTargetDatabaseIfExists(string connectionString, MigrationSetting migrationSetting)
        {
            ConnectionExtensions.DropDatabaseIfExists(connectionString);
            return Task.CompletedTask;
        }
    }
}
