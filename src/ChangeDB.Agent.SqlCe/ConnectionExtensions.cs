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
    }
}
