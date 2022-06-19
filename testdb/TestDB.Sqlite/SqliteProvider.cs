using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TestDB.Sqlite
{
    public class SqliteProvider : BaseServiceProvider
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
            return new SqliteConnectionStringBuilder(connectionString)
            {
                DataSource = $"{databaseName}.db"
            }.ConnectionString;
        }

        public override void CleanDatabase(string connectionString)
        {
            //using var connection = CreateConnection(connectionString);
            //DropAllForeignConstraints();
            //DropAllSchemas();
            //void DropAllSchemas()
            //{
            //    var allSchemas = connection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata where schema_owner = 'dbo'");
            //    allSchemas.ToList().ForEach(p => DropSchemaIfExists(p, p != "dbo"));
            //}

            //void DropSchemaIfExists(string schema, bool dropSchema = true)
            //{
            //    var allViews = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}' AND t.TABLE_TYPE ='VIEW'");
            //    allViews.ForEach(p => DropViewIfExists(schema, p));

            //    var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t WHERE t.TABLE_SCHEMA = '{schema}' AND t.TABLE_TYPE ='BASE TABLE'");
            //    allTables.ForEach(p => DropTableIfExists(schema, p));
            //    if (dropSchema)
            //    {
            //        connection.ExecuteNonQuery($"drop schema if exists {IdentityName(schema)}");
            //    }
            //}
            //void DropTableIfExists(string schema, string table)
            //{
            //    connection.ExecuteNonQuery($"drop table if exists {IdentityName(schema, table)}");
            //}
            //void DropViewIfExists(string schema, string view)
            //{
            //    connection.ExecuteNonQuery($"drop view if exists {IdentityName(schema, view)}");
            //}
            //void DropAllForeignConstraints()
            //{
            //    var allForeignConstraints = connection.ExecuteReaderAsList<string, string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY'");
            //    allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"ALTER TABLE {IdentityName(p.Item3, p.Item1)} drop constraint {IdentityName(p.Item2)};"));
            //}
        }

        public override void CloneDatabase(string connectionString, string newDatabaseName)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            newConnection.ExecuteNonQuery(
                 $"DBCC CLONEDATABASE ({IdentityName(GetDatabaseName(connectionString))}, {IdentityName(newDatabaseName)})");
            newConnection.ExecuteNonQuery($"ALTER DATABASE {IdentityName(GetDatabaseName(connectionString))} SET READ_WRITE WITH NO_WAIT");

        }

        public override DbConnection CreateConnection(string connectionString)
        {
            return new SqliteConnection(connectionString);
        }

        public override void CreateDatabase(string connectionString)
        {
            Console.WriteLine($"create database '{connectionString}'");
        }

        public override void DropTargetDatabaseIfExists(string connectionString)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            var fileName = builder.DataSource;
            lock (fileName)
            {
                if (File.Exists(fileName))
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(fileName);
                }
            }
        }

        public override string MakeReadOnly(string connectionString)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            newConnection.ExecuteNonQuery($"ALTER DATABASE {IdentityName(GetDatabaseName(connectionString))} SET READ_ONLY WITH NO_WAIT");
            return connectionString;
        }

        protected override bool IsSplitLine(string line)
        {
            return "go".Equals(line?.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetDatabaseName(string connectionString)
        {
            return new SqliteConnectionStringBuilder(connectionString).DataSource;
        }
        private DbConnection CreateNoDatabaseConnection(string connection)
        {
            return new SqliteConnection(ChangeDatabase(connection, string.Empty));
        }
    }

}
