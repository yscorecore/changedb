using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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
        public static void DropDatabaseIfExists(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
                newConnection.ExecuteNonQuery(
                     $"drop database if exists {connectionInfo.Database}"
                     );
            }
        }
        public static void CreateDatabase(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
                newConnection.ExecuteNonQuery($"create database {connectionInfo.Database}");
            }
        }

        public static void ClearDatabase(this DbConnection connection)
        {
            connection.DropAllForeignConstraints();
           connection.DropAllSchemas();
        }

        private static void DropAllForeignConstraints(this DbConnection connection)
        {
            var allForeignConstraints = connection.ExecuteReaderAsList<string,string,string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY'");
            allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"alter table \"{p.Item3}\".\"{p.Item1}\" drop constraint \"{p.Item2}\";"));
        }

        private static void DropAllSchemas(this DbConnection connection)
        {
            static bool IsSystemSchema(string schema) => schema.StartsWith("pg_") || schema == "information_schema";
            var allSchemas = connection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata");
            allSchemas.Where(p=>!IsSystemSchema(p)).ForEach(p => connection.DropSchemaIfExists(p, p != "public"));
        }

        private static void DropSchemaIfExists(this DbConnection connection, string schema, bool dropSchema = true)
        {

            var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}'");
            allTables.ForEach(p => connection.DropTableIfExists(schema, p));
            if (dropSchema)
            {
                connection.ExecuteNonQuery($"drop schema if exists \"{schema}\"");
            }
        }
        public static void DropTableIfExists(this DbConnection connection, string schema, string table)
        {
            connection.ExecuteNonQuery($"drop table if exists \"{schema}\".\"{table}\"");
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
