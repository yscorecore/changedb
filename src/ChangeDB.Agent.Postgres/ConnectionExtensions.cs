using System.Data;
using System.Data.Common;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public static class ConnectionExtensions
    {
        public static void ReCreateDatabase(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
                newConnection.ExecuteNonQuery(
                    $"drop database if exists {connectionInfo.Database}",
                            $"create database {connectionInfo.Database}"
                    );
            }
        }
        public static int DropDatabaseIfExists(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
                return newConnection.ExecuteNonQuery(
                     $"drop database if exists {connectionInfo.Database}"
                     );
            }
        }
        public static int CreateDatabase(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
                return newConnection.ExecuteNonQuery($"create database {connectionInfo.Database}");
            }
        }
        public static string ExtractDatabaseName(this DbConnection connection)
        {
            var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            return connectionInfo.Database;
        }

        private static NpgsqlConnection CreateNoDatabaseConnection(IDbConnection connection)
        {
            var builder = new NpgsqlConnectionStringBuilder(connection.ConnectionString) { Database = null };
            var newConnection = new NpgsqlConnection(builder.ConnectionString);
            return newConnection;
        }
    }
}
