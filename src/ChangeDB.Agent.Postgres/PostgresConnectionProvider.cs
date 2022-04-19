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

        public DbConnection CreateConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
