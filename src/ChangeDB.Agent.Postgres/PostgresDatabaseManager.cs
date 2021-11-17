using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new PostgresDatabaseManager();
        public Task DropDatabaseIfExists(DbConnection connection, MigrationSetting migrationSetting)
        {
            connection.DropDatabaseIfExists();
            return Task.CompletedTask;
        }

        public Task CreateDatabase(DbConnection connection, MigrationSetting migrationSetting)
        {
            connection.CreateDatabase();
            return Task.CompletedTask;
        }
    }
}
