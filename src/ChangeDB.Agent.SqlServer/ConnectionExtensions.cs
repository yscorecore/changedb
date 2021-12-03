using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{
    public static class ConnectionExtensions
    {
        public static void DropDatabaseIfExists(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new SqlConnectionStringBuilder(connection.ConnectionString);
                newConnection.ExecuteNonQuery(
                     $" drop database  if exists {SqlServerUtils.IdentityName(connectionInfo.InitialCatalog)}"
                     );
            }
        }
        public static void CreateDatabase(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new SqlConnectionStringBuilder(connection.ConnectionString);
                newConnection.ExecuteNonQuery($"create database {SqlServerUtils.IdentityName(connectionInfo.InitialCatalog)}");
            }
        }




        public static void ClearDatabase(this DbConnection connection)
        {
            connection.DropAllForeignConstraints();
            connection.DropAllSchemas();
        }
        private static void DropAllForeignConstraints(this DbConnection connection)
        {
            var allForeignConstraints = connection.ExecuteReaderAsList<string, string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY'");
            allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"alter table {SqlServerUtils.IdentityName(p.Item3,p.Item1)} drop constraint {SqlServerUtils.IdentityName(p.Item2)};"));
        }

        private static void DropAllSchemas(this DbConnection connection)
        {
            static bool IsSystemSchema(string schema) => schema.StartsWith("pg_") || schema == "information_schema";
            var allSchemas = connection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata where schema_owner = 'dbo'");
            allSchemas.Where(p => !IsSystemSchema(p)).Each(p => connection.DropSchemaIfExists(p, p != "dbo"));
        }
        private static DbConnection CreateNoDatabaseConnection(IDbConnection connection)
        {
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString) { InitialCatalog = string.Empty };
            return new SqlConnection(builder.ConnectionString);
        }
        public static void DropSchemaIfExists(this DbConnection connection, string schema, bool dropSchema = true)
        {
            var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}'");
            allTables.ForEach(p => connection.DropTableIfExists(schema, p));
            if (dropSchema)
            {
                connection.ExecuteNonQuery($"drop schema if exists {SqlServerUtils.IdentityName(schema)}");
            }
        }
        public static void DropTableIfExists(this DbConnection connection, string schema, string table)
        {
            connection.ExecuteNonQuery($"drop table if exists {SqlServerUtils.IdentityName(schema,table)}");
        }
    }
}
