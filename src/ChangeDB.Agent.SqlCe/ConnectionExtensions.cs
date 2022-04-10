using System.Data;
using System.Data.SqlServerCe;
using System.IO;

namespace ChangeDB.Agent.SqlCe
{
    public static class ConnectionExtensions
    {
        public static void ReCreateDatabase(this IDbConnection connection)
        {
            DropDatabaseIfExists(connection);
            CreateDatabase(connection);
        }
        public static void DropDatabaseIfExists(this IDbConnection connection)
        {
            SqlCeConnectionStringBuilder builder = new SqlCeConnectionStringBuilder(connection.ConnectionString);
            var fileName = builder.DataSource;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
        public static void DropDatabaseIfExists(string connectionString)
        {
            var builder = new SqlCeConnectionStringBuilder(connectionString);
            var fileName = builder.DataSource;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
        public static void CreateDatabase(this IDbConnection connection)
        {
            using var engine = new SqlCeEngine(connection.ConnectionString);
            engine.CreateDatabase();
        }
        public static void CreateDatabase(string connectionString)
        {
            SqlCeEngine engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();
        }

        public static void ClearDatabase(this IDbConnection connection)
        {
            //System.Diagnostics.Debugger.Launch();
            connection.DropAllForeignConstraints();
            connection.DropAllTables();
        }
        private static void DropAllForeignConstraints(this IDbConnection connection)
        {
            var allForeignConstraints = connection.ExecuteReaderAsList<string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY'");
            allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"ALTER TABLE [{p.Item1}] drop constraint [{p.Item2}];"));
        }


        public static void DropAllTables(this IDbConnection connection)
        {
            var allTables = connection.ExecuteReaderAsList<string>($"SELECT table_name from INFORMATION_SCHEMA.TABLES t;");
            allTables.ForEach(p => connection.DropTable(p));

        }
        public static void DropTable(this IDbConnection connection, string table)
        {
            connection.ExecuteNonQuery($"drop table {SqlCeUtils.IdentityName(table)}");
        }
    }
}
