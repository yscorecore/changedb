using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;
using static ChangeDB.Agent.SqlServer.SqlServerUtils;
namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new SqlServerDatabaseManager();

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
            var databaseName = GetDatabaseName(connectionString);

            using (var connection = new SqlConnection(connectionString))
            {
                SqlConnection.ClearPool(connection);
            }
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var spids = newConnection.ExecuteReaderAsList<int>($"select spid from sysprocesses WHERE dbid = db_id('{databaseName}')");
            foreach (var spid in spids)
            {
                newConnection.ExecuteNonQuery($"kill {spid}");
            }
            var connectionInfo = new SqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery(
                $" drop database if exists {IdentityName(connectionInfo.InitialCatalog)}"
            );
        }


        private static void CreateDatabase(string connection)
        {
            using var newConnection = CreateNoDatabaseConnection(connection);
            var connectionInfo = new SqlConnectionStringBuilder(connection);
            newConnection.ExecuteNonQuery($"create database {IdentityName(connectionInfo.InitialCatalog)}");
        }

        private static DbConnection CreateNoDatabaseConnection(string connection)
        {
            var builder = new SqlConnectionStringBuilder(connection) { InitialCatalog = string.Empty };
            return new SqlConnection(builder.ConnectionString);
        }
        private static string GetDatabaseName(string connectionString)
        {
            return new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        }
    }
}
