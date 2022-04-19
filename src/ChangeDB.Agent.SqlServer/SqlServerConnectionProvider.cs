using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerConnectionProvider : IConnectionProvider
    {
        public static readonly IConnectionProvider Default = new SqlServerConnectionProvider();

        public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);
    }
}
