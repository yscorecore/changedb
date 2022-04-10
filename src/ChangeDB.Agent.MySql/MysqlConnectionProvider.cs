using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

namespace ChangeDB.Agent.MySql
{
    public class MysqlConnectionProvider : IConnectionProvider
    {
        public static readonly IConnectionProvider Default = new MysqlConnectionProvider();
        public string ChangeDatabase(string connectionString, string databaseName)
        {
            return new MySqlConnectionStringBuilder(connectionString)
            {
                Database = databaseName
            }.ToString();
        }

        public DbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }
    }
}
