using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{
    public static class ConnectionExtensions
    {
       
        public static void DropDatabaseIfExists(string connectionString)
        {
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
                 $" drop database if exists {SqlServerUtils.IdentityName(connectionInfo.InitialCatalog)}"
                 );
        }

        public static void CreateDatabase(string connection)
        {
            using var newConnection = CreateNoDatabaseConnection(connection);
            var connectionInfo = new SqlConnectionStringBuilder(connection);
            newConnection.ExecuteNonQuery($"create database {SqlServerUtils.IdentityName(connectionInfo.InitialCatalog)}");
        }

        private static DbConnection CreateNoDatabaseConnection(string connection)
        {
            var builder = new SqlConnectionStringBuilder(connection) { InitialCatalog = string.Empty };
            return new SqlConnection(builder.ConnectionString);
        }

    }
}
