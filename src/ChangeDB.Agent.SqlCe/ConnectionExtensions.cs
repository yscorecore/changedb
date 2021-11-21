using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Agent.SqlCe
{
    public static class ConnectionExtensions
    {
        public static void ReCreateDatabase(this DbConnection connection)
        {
            DropDatabaseIfExists(connection);
            CreateDatabase(connection);
        }
        public static void DropDatabaseIfExists(this DbConnection connection)
        {
            SqlCeConnectionStringBuilder builder = new SqlCeConnectionStringBuilder(connection.ConnectionString);
            var fileName = builder.DataSource;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
        public static void CreateDatabase(this DbConnection connection)
        {
            SqlCeEngine engine = new SqlCeEngine(connection.ConnectionString);
            engine.CreateDatabase();
        }

        public static void ClearDatabase(this DbConnection connection)
        {
            //System.Diagnostics.Debugger.Launch();
            //connection.DropAllForeignConstraints();
            connection.DropAllTables();
        }
        private static void DropAllForeignConstraints(this DbConnection connection)
        {
            var allForeignConstraints = connection.ExecuteReaderAsList<string, string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY'");
            allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"alter table \"{p.Item3}\".\"{p.Item1}\" drop constraint \"{p.Item2}\";"));
        }


        public static void DropAllTables(this DbConnection connection)
        {
            var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t;");
            allTables.ForEach(p => connection.DropTable(p));
           
        }
        public static void DropTable(this DbConnection connection,string table)
        {
            connection.ExecuteNonQuery($"drop table {SqlCeUtils.IdentityName(table)}");
        }
    }
}
