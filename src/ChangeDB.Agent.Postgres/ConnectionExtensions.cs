using System.Data;
using System.Data.Common;
using System.Linq;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    internal static class ConnectionExtensions
    {
        public static void DropDatabaseIfExists(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                NpgsqlConnection.ClearPool(connection);
            }
            var databaseName = GetDatabaseName(connectionString);
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            newConnection.ExecuteNonQuery(@$"
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE pid <> pg_backend_pid() and datname ='{databaseName}'");
            newConnection.ExecuteNonQuery(
                 $"drop database if exists {PostgresUtils.IdentityName(databaseName)}"
                 );
        }
        private static string GetDatabaseName(string connectionString)
        {
            return new NpgsqlConnectionStringBuilder(connectionString).Database;
        }
        public static void CreateDatabase(string connectionString)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new NpgsqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery($"CREATE DATABASE {PostgresUtils.IdentityName(connectionInfo.Database)};");
        }

        private static NpgsqlConnection CreateNoDatabaseConnection(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
            var newConnection = new NpgsqlConnection(builder.ConnectionString);
            return newConnection;
        }
    }
}
