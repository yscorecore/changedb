using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace ChangeDB.Agent.Sqlite
{
    public class SqliteConnectionProvider : IConnectionProvider
    {
        public static readonly IConnectionProvider Default = new SqliteConnectionProvider();

        public DbConnection CreateConnection(string connectionString) => new SqliteConnection(connectionString);
    }
}
