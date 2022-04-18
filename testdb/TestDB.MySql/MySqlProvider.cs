using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

namespace TestDB.MySql
{
    public class MySqlProvider : BaseServiceProvider
    {
        private static string IdentityName(string name)
        {
            return $"`{name}`";
        }
        private static string IdentityName(string schema, string name)
        {
            return string.IsNullOrEmpty(schema) ? IdentityName(name) : $"{IdentityName(schema)}.{IdentityName(name)}";
        }
        public override bool SupportFastClone => false;

        public override string ChangeDatabase(string connectionString, string databaseName)
        {
            return new MySqlConnectionStringBuilder(connectionString)
            {
                Database = databaseName,
            }.ConnectionString;
        }

        public override void CleanDatabase(string connectionString)
        {
            using var connection = CreateConnection(connectionString);
            DropAllForeignConstraints();
            DropAllViews();
            DropAllTables();
            void DropAllViews()
            {
                var allViews = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t where t.TABLE_SCHEMA =database() and TABLE_TYPE ='VIEW';");
                allViews.ForEach(p => DropView(p));
            }

            void DropAllTables()
            {
                var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t where t.TABLE_SCHEMA =database() and TABLE_TYPE ='BASE TABLE';");
                allTables.ForEach(p => DropTable(p));
            }
            void DropTable(string table)
            {
                connection.ExecuteNonQuery($"drop table {IdentityName(table)}");
            }
            void DropView(string view)
            {
                connection.ExecuteNonQuery($"drop view {IdentityName(view)}");
            }

            void DropAllForeignConstraints()
            {
                var allForeignConstraints = connection.ExecuteReaderAsList<string, string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY';");
                allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"ALTER TABLE {IdentityName(p.Item3, p.Item1)} drop constraint {IdentityName(p.Item2)};"));
            }
        }

        public override void CloneDatabase(string connectionString, string newDatabaseName)
        {
            throw new NotSupportedException();

        }

        public override DbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
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
            using (var connection = new MySqlConnection(connectionString))
            {
                MySqlConnection.ClearPool(connection);
            }
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            newConnection.ExecuteNonQuery(
                 $"DROP DATABASE IF EXISTS {IdentityName(GetDatabaseName(connectionString))}"
                 );
        }

        public override string MakeReadOnly(string connectionString)
        {
            // TODO create readonly account  
            return connectionString;
        }

        protected override bool IsSplitLine(string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }

        private string GetDatabaseName(string connectionString)
        {
            return new MySqlConnectionStringBuilder(connectionString).Database;
        }
        private DbConnection CreateNoDatabaseConnection(string connection)
        {
            return new MySqlConnection(ChangeDatabase(connection, "sys"));
        }
    }

}
