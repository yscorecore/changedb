using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeConnectionProvider : IConnectionProvider
    {
        public static readonly IConnectionProvider Default = new SqlCeConnectionProvider();
        public string ChangeDatabase(string connectionString, string databaseName)
        {
            return new SqlCeConnectionStringBuilder(connectionString)
            {
                DataSource = $"{databaseName}"
            }.ToString();
        }

        public DbConnection CreateConnection(string connectionString)
        {
            return new SqlCeConnection(connectionString);
        }
    }
}
