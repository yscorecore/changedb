using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new PostgresDatabaseManager();
        public Task DropDatabaseIfExists(DbConnection connection, MigrationContext migrationContext)
        {
            connection.DropDatabaseIfExists();
            return Task.CompletedTask;
        }

        public Task CreateDatabase(DbConnection connection, MigrationContext migrationContext)
        {
            connection.CreateDatabase();
            return Task.CompletedTask;
        }
    }
}
