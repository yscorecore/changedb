using System.Data;
using System.Data.Common;
using MySqlConnector;
using static ChangeDB.Agent.MySql.MySqlUtils;
namespace ChangeDB.Agent.MySql
{
    public static class ConnectionExtensions
    {


        public static void DropDatabaseIfExists(string connectionString)
        {

            using (var connection = new MySqlConnection(connectionString))
            {
                MySqlConnection.ClearPool(connection);
            }
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new MySqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery(
                $"DROP DATABASE IF EXISTS {IdentityName(connectionInfo.Database)};"
            );
        }


        public static void CreateDatabase(string connectionString)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new MySqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery($"CREATE DATABASE {IdentityName(connectionInfo.Database)};");
        }










        private static MySqlConnection CreateNoDatabaseConnection(string connection)
        {
            var builder = new MySqlConnectionStringBuilder(connection) { Database = "sys" };
            var newConnection = new MySqlConnection(builder.ConnectionString);
            return newConnection;
        }
    }
}
