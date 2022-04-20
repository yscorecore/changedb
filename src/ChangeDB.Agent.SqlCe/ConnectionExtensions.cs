using System.Data;
using System.Data.SqlServerCe;
using System.IO;

namespace ChangeDB.Agent.SqlCe
{
    internal static class ConnectionExtensions
    {
        public static void DropDatabaseIfExists(string connectionString)
        {
            var builder = new SqlCeConnectionStringBuilder(connectionString);
            var fileName = builder.DataSource;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        public static void CreateDatabase(string connectionString)
        {
            SqlCeEngine engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();
        }
    }
}
