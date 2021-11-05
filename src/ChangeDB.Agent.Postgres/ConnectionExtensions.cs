using System.Data;
using System.Data.Common;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public static class ConnectionExtensions
    {
        public static void ReCreateDatabase(this IDbConnection connection)
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

        private static NpgsqlConnection CreateNoDatabaseConnection(IDbConnection connection)
        {
            var builder = new NpgsqlConnectionStringBuilder(connection.ConnectionString) { Database = null };
            var newConnection = new NpgsqlConnection(builder.ConnectionString);
            return newConnection;
        }
    }
}
