using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresConnectionProvider : IConnectionProvider
    {
        public static readonly IConnectionProvider Default = new PostgresConnectionProvider();
        public string ChangeDatabase(string connectionString, string databaseName)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            builder.Database = databaseName;
            return builder.ToString();
        }

        public DbConnection CreateConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
