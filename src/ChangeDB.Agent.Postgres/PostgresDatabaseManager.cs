using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Npgsql;
using static ChangeDB.Agent.Postgres.PostgresUtils;
namespace ChangeDB.Agent.Postgres
{
    public class PostgresDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new PostgresDatabaseManager();


        public Task CreateDatabase(string connectionString)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new NpgsqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery($"CREATE DATABASE {PostgresUtils.IdentityName(connectionInfo.Database)};");
            return Task.CompletedTask;
        }

        public Task DropDatabaseIfExists(string connectionString)
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
            return Task.CompletedTask;
        }

       
        private static string GetDatabaseName(string connectionString)
        {
            return new NpgsqlConnectionStringBuilder(connectionString).Database;
        }

    

        private static NpgsqlConnection CreateNoDatabaseConnection(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
            var newConnection = new NpgsqlConnection(builder.ConnectionString);
            return newConnection;
        }
    }
}
