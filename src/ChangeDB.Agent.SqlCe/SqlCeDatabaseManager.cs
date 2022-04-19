using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.SqlCe.SqlCeUtils;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new SqlCeDatabaseManager();

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
