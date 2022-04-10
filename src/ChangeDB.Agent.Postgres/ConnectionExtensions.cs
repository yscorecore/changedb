using System.Data;
using System.Data.Common;
using System.Linq;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public static class ConnectionExtensions
    {
        public static void ReCreateDatabase(this IDbConnection connection)
        {
            using var newConnection = CreateNoDatabaseConnection(connection);
            var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            newConnection.ExecuteNonQuery(
                $"DROP DATABASE IF EXISTS {PostgresUtils.IdentityName(connectionInfo.Database)};",
                $"CREATE DATABASE {PostgresUtils.IdentityName(connectionInfo.Database)};"
            );
        }
        public static void DropDatabaseIfExists(this IDbConnection connection)
        {
            NpgsqlConnection.ClearPool((NpgsqlConnection)connection);
            var databaseName = connection.Database;
            connection.ChangeDatabase("postgres");
            //connection.Open();
            connection.ExecuteNonQuery(
                $"DROP DATABASE IF EXISTS {PostgresUtils.IdentityName(databaseName)};"
            );
            using var newConnection = CreateNoDatabaseConnection(connection);
            var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            newConnection.ExecuteNonQuery(
                $"DROP DATABASE IF EXISTS {PostgresUtils.IdentityName(connectionInfo.Database)};"
            );
        }

        public static void DropDatabaseIfExists(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                NpgsqlConnection.ClearPool(connection);
            }
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new NpgsqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery(
                $"DROP DATABASE IF EXISTS {PostgresUtils.IdentityName(connectionInfo.Database)};"
            );
        }
        public static void CreateDatabase(this IDbConnection connection)
        {
            using var newConnection = CreateNoDatabaseConnection(connection);
            var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            newConnection.ExecuteNonQuery($"CREATE DATABASE {PostgresUtils.IdentityName(connectionInfo.Database)};");
        }
        public static void CreateDatabase(string connectionString)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new NpgsqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery($"CREATE DATABASE {PostgresUtils.IdentityName(connectionInfo.Database)};");
        }

        public static void ClearDatabase(this IDbConnection connection)
        {
            connection.DropAllForeignConstraints();
            connection.DropAllSchemas();
        }

        private static void DropAllForeignConstraints(this IDbConnection connection)
        {
            var allForeignConstraints = connection.ExecuteReaderAsList<string, string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY';");
            allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"ALTER TABLE {PostgresUtils.IdentityName(p.Item3, p.Item1)} drop constraint {PostgresUtils.IdentityName(p.Item2)};"));
        }

        private static void DropAllSchemas(this IDbConnection connection)
        {
            static bool IsSystemSchema(string schema) => schema.StartsWith("pg_") || schema == "information_schema";
            var allSchemas = connection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata");
            allSchemas.Where(p => !IsSystemSchema(p)).Each(p => connection.DropSchemaIfExists(p, p != "public"));
        }

        private static void DropSchemaIfExists(this IDbConnection connection, string schema, bool dropSchema = true)
        {
            var allViews = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}' AND t.TABLE_TYPE='VIEW'");
            allViews.ForEach(p => connection.DropViewIfExists(schema, p));
            var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}' AND t.TABLE_TYPE='BASE TABLE'");
            allTables.ForEach(p => connection.DropTableIfExists(schema, p));
            if (dropSchema)
            {
                connection.ExecuteNonQuery($"DROP SCHEMA IF EXISTS {PostgresUtils.IdentityName(schema)};");
            }
        }
        public static void DropTableIfExists(this IDbConnection connection, string schema, string table)
        {
            connection.ExecuteNonQuery($"DROP TABLE IF EXISTS {PostgresUtils.IdentityName(schema, table)};");
        }
        public static void DropViewIfExists(this IDbConnection connection, string schema, string table)
        {
            connection.ExecuteNonQuery($"DROP VIEW IF EXISTS {PostgresUtils.IdentityName(schema, table)};");
        }
        public static string ExtractDatabaseName(this IDbConnection connection)
        {
            var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            return connectionInfo.Database;
        }

        private static NpgsqlConnection CreateNoDatabaseConnection(IDbConnection connection)
        {
            var builder = new NpgsqlConnectionStringBuilder(connection.ConnectionString) { Database = "postgres" };
            var newConnection = new NpgsqlConnection(builder.ConnectionString);
            return newConnection;
        }
        private static NpgsqlConnection CreateNoDatabaseConnection(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
            var newConnection = new NpgsqlConnection(builder.ConnectionString);
            return newConnection;
        }
    }
}
