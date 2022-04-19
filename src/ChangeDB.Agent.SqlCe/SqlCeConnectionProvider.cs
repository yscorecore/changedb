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

        public DbConnection CreateConnection(string connectionString)
        {
            _ = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            return new SqlCeConnection(connectionString);
        }
    }
}
