using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{
    public static class ConnectionExtensions
    {
        // public static void ReCreateDatabase(this DbConnection connection)
        // {
        //     using (var newConnection = CreateNoDatabaseConnection(connection))
        //     {
        //         var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
        //         newConnection.ExecuteNonQuery(
        //             $"drop database if exists {connectionInfo.Database}",
        //                     $"create database {connectionInfo.Database}"
        //             );
        //     }
        // }
        public static int DropDatabaseIfExists(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new SqlConnectionStringBuilder(connection.ConnectionString);
                return newConnection.ExecuteNonQuery(
                     $" drop database  if exists [{connectionInfo.InitialCatalog}]"
                     );
            }
        }
        public static int CreateDatabase(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new SqlConnectionStringBuilder(connection.ConnectionString);
                return newConnection.ExecuteNonQuery($"create database {connectionInfo.InitialCatalog}");
            }
        }


        private static DbConnection CreateNoDatabaseConnection(IDbConnection connection)
        {
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString) { InitialCatalog=string.Empty  };
            return new SqlConnection(builder.ConnectionString);
        }
    }
}
