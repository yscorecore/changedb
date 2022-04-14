using System.Data;
using System.Data.Common;
using MySqlConnector;
using static ChangeDB.Agent.MySql.MySqlUtils;
namespace ChangeDB.Agent.MySql
{
    public static class ConnectionExtensions
    {
        public static void ReCreateDatabase(this IDbConnection connection)
        {
            using var newConnection = CreateNoDatabaseConnection(connection);
            var connectionInfo = new MySqlConnectionStringBuilder(connection.ConnectionString);
            newConnection.ExecuteNonQuery(
                $"DROP DATABASE IF EXISTS {IdentityName(connectionInfo.Database)};",
                $"CREATE DATABASE {IdentityName(connectionInfo.Database)};"
            );
        }
        public static void DropDatabaseIfExists(this IDbConnection connection)
        {
            using var newConnection = CreateNoDatabaseConnection(connection);
            var connectionInfo = new MySqlConnectionStringBuilder(connection.ConnectionString);
            newConnection.ExecuteNonQuery(
                $"DROP DATABASE IF EXISTS {IdentityName(connectionInfo.Database)};"
            );
        }

        public static void DropDatabaseIfExists(string connectionString)
        {

            using (var connection = new MySqlConnection(connectionString))
            {
                MySqlConnection.ClearPool(connection);
            }
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new MySqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery(
                $"DROP DATABASE IF EXISTS {IdentityName(connectionInfo.Database)};"
            );
        }
        public static void CreateDatabase(this IDbConnection connection)
        {
            using var newConnection = CreateNoDatabaseConnection(connection);
            var connectionInfo = new MySqlConnectionStringBuilder(connection.ConnectionString);
            newConnection.ExecuteNonQuery($"CREATE DATABASE {IdentityName(connectionInfo.Database)};");
        }

        public static void CreateDatabase(string connectionString)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new MySqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery($"CREATE DATABASE {IdentityName(connectionInfo.Database)};");
        }

        public static void ClearDatabase(this IDbConnection connection)
        {
            connection.DropAllForeignConstraints();
            connection.DropAllTables();
        }

        private static void DropAllForeignConstraints(this IDbConnection connection)
        {
            var allForeignConstraints = connection.ExecuteReaderAsList<string, string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY';");
            allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"ALTER TABLE {IdentityName(p.Item3, p.Item1)} drop constraint {IdentityName(p.Item2)};"));
        }



        public static void DropAllTables(this IDbConnection connection)
        {
            var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t where t.TABLE_SCHEMA ='{connection.ExtractDatabaseName()}';");
            allTables.ForEach(p => connection.DropTable(p));

        }
        public static void DropTable(this IDbConnection connection, string table)
        {
            connection.ExecuteNonQuery($"drop table {IdentityName(table)}");
        }


        public static string ExtractDatabaseName(this IDbConnection connection)
        {
            var connectionInfo = new MySqlConnectionStringBuilder(connection.ConnectionString);
            return connectionInfo.Database;
        }

        private static MySqlConnection CreateNoDatabaseConnection(IDbConnection connection)
        {
            var builder = new MySqlConnectionStringBuilder(connection.ConnectionString) { Database = "sys" };
            var newConnection = new MySqlConnection(builder.ConnectionString);
            return newConnection;
        }
        private static MySqlConnection CreateNoDatabaseConnection(string connection)
        {
            var builder = new MySqlConnectionStringBuilder(connection) { Database = "sys" };
            var newConnection = new MySqlConnection(builder.ConnectionString);
            return newConnection;
        }
    }
}
