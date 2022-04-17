using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace TestDB.Postgres
{
    public class PostgresProvider : BaseServiceProvider
    {
        private static string IdentityName(string name)
        {
            return $"\"{name}\"";
        }
        private static string IdentityName(string schema, string name)
        {
            return string.IsNullOrEmpty(schema) ? IdentityName(name) : $"{IdentityName(schema)}.{IdentityName(name)}";
        }
        public override bool SupportFastClone => true;

        public override string ChangeDatabase(string connectionString, string databaseName)
        {
            return new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = databaseName
            }.ConnectionString;
        }

        public override void CleanDatabase(string connectionString)
        {
            using var connection = CreateConnection(connectionString);
            DropAllForeignConstraints();
            DropAllSchemas();
            void DropAllSchemas()
            {
                static bool IsSystemSchema(string schema) => schema.StartsWith("pg_") || schema == "information_schema";
                var allSchemas = connection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata");
                allSchemas.Where(p => !IsSystemSchema(p)).ToList().ForEach(p => DropSchemaIfExists(p, p != "public"));
            }

            void DropSchemaIfExists(string schema, bool dropSchema = true)
            {
                var allViews = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}' AND t.TABLE_TYPE='VIEW'");
                allViews.ForEach(p => DropViewIfExists(schema, p));
                var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}' AND t.TABLE_TYPE='BASE TABLE'");
                allTables.ForEach(p => DropTableIfExists(schema, p));

                if (dropSchema)
                {
                    connection.ExecuteNonQuery($"drop schema if exists {IdentityName(schema)}");
                }
            }
            void DropTableIfExists(string schema, string table)
            {
                connection.ExecuteNonQuery($"drop table if exists {IdentityName(schema, table)}");
            }
            void DropViewIfExists( string schema, string view)
            {
                connection.ExecuteNonQuery($"DROP VIEW IF EXISTS {IdentityName(schema, view)};");
            }
            void DropAllForeignConstraints()
            {
                var allForeignConstraints = connection.ExecuteReaderAsList<string, string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY';");
                allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"ALTER TABLE {IdentityName(p.Item3, p.Item1)} drop constraint {IdentityName(p.Item2)};"));

            }
        }

        public override void CloneDatabase(string connectionString, string newDatabaseName)
        {
            var templateDbName = GetDatabaseName(connectionString);
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            newConnection.ExecuteNonQuery(
                 $"CREATE DATABASE {IdentityName(newDatabaseName)} WITH TEMPLATE {IdentityName(templateDbName)};");
        }

        public override IDbConnection CreateConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        public override void CreateDatabase(string connectionString)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            newConnection.ExecuteNonQuery(
                 $"create database {IdentityName(GetDatabaseName(connectionString))}"
                 );
        }

        public override void DropTargetDatabaseIfExists(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                NpgsqlConnection.ClearPool(connection);
            }
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            newConnection.ExecuteNonQuery(
                 $"drop database if exists {IdentityName(GetDatabaseName(connectionString))}"
                 );
        }

        public override string MakeReadOnly(string connectionString)
        {
            // TODO readonly mode
            return connectionString;
        }

        protected override bool IsSplitLine(string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }

        private string GetDatabaseName(string connectionString)
        {
            return new NpgsqlConnectionStringBuilder(connectionString).Database;
        }
        private DbConnection CreateNoDatabaseConnection(string connection)
        {
            return new NpgsqlConnection(ChangeDatabase(connection, "postgres"));
        }
    }

}
