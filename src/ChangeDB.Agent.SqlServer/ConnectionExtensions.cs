using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
                     $" drop database  if exists [{connectionInfo.InitialCatalog}]"
                     );
            }
        }
        public static void CreateDatabase(this DbConnection connection)
        {
            using (var newConnection = CreateNoDatabaseConnection(connection))
            {
                var connectionInfo = new SqlConnectionStringBuilder(connection.ConnectionString);
                newConnection.ExecuteNonQuery($"create database {connectionInfo.InitialCatalog}");
            }
        }


        private static DbConnection CreateNoDatabaseConnection(IDbConnection connection)
        {
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString) { InitialCatalog = string.Empty };
            return new SqlConnection(builder.ConnectionString);
        }


        public static void ClearDatabase(this DbConnection connection)
        {
            var systemSchemas = new List<string> { "dbo" };
            var allSchemas = connection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata where schema_owner = 'dbo'");
            allSchemas.ForEach(p => connection.DropSchemaIfExists(p, p != "dbo"));
        }
        public static void DropSchemaIfExists(this DbConnection connection, string schema, bool dropSchema = true)
        {
            var allForeignConstraints = connection.ExecuteReaderAsList<string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY' and TABLE_SCHEMA ='{schema}'");

            allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"alter table [{schema}].[{p.Item1}] drop constraint {p.Item2};"));

            var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}'");
            allTables.ForEach(p => connection.DropTableIfExists(schema, p));
            if (dropSchema)
            {
                connection.ExecuteNonQuery($"drop schema if exists [{schema}]");
            }
        }
        public static void DropTableIfExists(this DbConnection connection, string schema, string table)
        {
            connection.ExecuteNonQuery($"drop table if exists [{schema}].[{table}]");
        }
    }
}
