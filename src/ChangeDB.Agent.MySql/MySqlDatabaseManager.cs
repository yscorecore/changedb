using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.MySql.MySqlUtils;
namespace ChangeDB.Agent.MySql
{
    public class MySqlDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new MySqlDatabaseManager();


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
