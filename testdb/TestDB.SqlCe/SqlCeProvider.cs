using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB.SqlCe
{
    public class SqlCeProvider : BaseServiceProvider
    {
        private static string IdentityName(string name)
        {
            return $"\"{name}\"";
        }
        private static string IdentityName(string schema, string name)
        {
            return string.IsNullOrEmpty(schema) ? IdentityName(name) : $"{IdentityName(schema)}.{IdentityName(name)}";
        }
        private static string BuildDatabaseFile(string databaseName)
        {
            return $"{databaseName}.mdf";
        }
        public override bool SupportFastClone => true;

        public override string ChangeDatabase(string connectionString, string databaseName)
        {
            return new SqlCeConnectionStringBuilder(connectionString)
            {
                DataSource = BuildDatabaseFile(databaseName)
            }.ConnectionString;
        }

        public override void CleanDatabase(string connectionString)
        {
            using var connection = CreateConnection(connectionString);
            DropAllForeignConstraints();
            DropAllViews();
            DropAllTables();


            void DropAllForeignConstraints()
            {
                var allForeignConstraints = connection.ExecuteReaderAsList<string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY'");
                allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"ALTER TABLE [{p.Item1}] drop constraint [{p.Item2}];"));
            }
            void DropAllViews()
            {
                var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t where t.TABLE_TYPE='VIEW';");
                allTables.ForEach(p => DropView(p));
            }
            void DropView(string view)
            {
                connection.ExecuteNonQuery($"drop view {IdentityName(view)}");
            }
            void DropAllTables()
            {
                var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t where t.TABLE_TYPE='BASE TABLE';");
                allTables.ForEach(p => DropTable(p));
            }
            void DropTable(string table)
            {
                connection.ExecuteNonQuery($"drop table {IdentityName(table)}");
            }
        }

        public override void CloneDatabase(string connectionString, string newDatabaseName)
        {
            var builder = new SqlCeConnectionStringBuilder(connectionString);
            var fileName = builder.DataSource;
            File.Copy(fileName, BuildDatabaseFile(newDatabaseName));
        }

        public override IDbConnection CreateConnection(string connectionString)
        {
            return new SqlCeConnection(connectionString);
        }

        public override void CreateDatabase(string connectionString)
        {
            SqlCeEngine engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();
        }

        public override void DropTargetDatabaseIfExists(string connectionString)
        {
            var builder = new SqlCeConnectionStringBuilder(connectionString);
            var fileName = builder.DataSource;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        public override string MakeReadOnly(string connectionString)
        {
            if (connectionString.EndsWith(';'))
            {
                return $"{connectionString}Mode = Read Only;Temp Path={ Path.GetTempPath()}";
            }
            else
            {
                return $"{connectionString};Mode = Read Only;Temp Path={ Path.GetTempPath()}";
            }
        }

        protected override bool IsSplitLine(string line)
        {
            return "go".Equals(line?.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }

    }

}
