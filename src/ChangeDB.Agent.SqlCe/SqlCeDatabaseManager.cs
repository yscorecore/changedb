using System.Data;
using System.Data.SqlServerCe;
using System.IO;
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
            CreateDatabase(connectionString);
            return Task.CompletedTask;
        }

        public Task DropTargetDatabaseIfExists(string connectionString, MigrationSetting migrationSetting)
        {
            DropDatabaseIfExists(connectionString);
            return Task.CompletedTask;
        }

        private static void DropDatabaseIfExists(string connectionString)
        {
            var builder = new SqlCeConnectionStringBuilder(connectionString);
            var fileName = builder.DataSource;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        private static void CreateDatabase(string connectionString)
        {
            SqlCeEngine engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();
        }
    }
}
