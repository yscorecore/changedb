using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Microsoft.Data.Sqlite;
using static ChangeDB.Agent.Sqlite.SqliteUtils;
namespace ChangeDB.Agent.Sqlite
{
    public class SqliteDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new SqliteDatabaseManager();

        public async Task CreateDatabase(string connectionString, MigrationSetting migrationSetting)
        {
            await CreateDatabase(connectionString);
        }

        public Task DropTargetDatabaseIfExists(string connectionString, MigrationSetting migrationSetting)
        {
            DropDatabaseIfExists(connectionString);
            return Task.CompletedTask;
        }

        public Task DropDatabaseIfExists(string connectionString)
        {
            var fileName = GetDatabaseName(connectionString);
            lock (fileName)
            {
                if (File.Exists(fileName))
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(fileName);
                }
            }
            return Task.CompletedTask;
        }


        public async Task CreateDatabase(string connection)
        {
            using var conn = CreateNoDatabaseConnection(connection);
            await conn.OpenAsync();
        }

        private static DbConnection CreateNoDatabaseConnection(string connection)
        {
            var builder = new SqliteConnectionStringBuilder(connection);
            return new SqliteConnection(builder.ConnectionString);
        }
        private static string GetDatabaseName(string connectionString)
        {
            return new SqliteConnectionStringBuilder(connectionString).DataSource;
        }
    }
}
