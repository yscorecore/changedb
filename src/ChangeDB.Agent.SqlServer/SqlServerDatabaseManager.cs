using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.SqlServer.SqlServerUtils;
namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new SqlServerDatabaseManager();

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
