using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using MySqlConnector;
using static ChangeDB.Agent.MySql.MySqlUtils;
namespace ChangeDB.Agent.MySql
{
    public class MySqlDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new MySqlDatabaseManager();


        public Task CreateDatabase(string connectionString)
        {
            using var newConnection = CreateNoDatabaseConnection(connectionString);
            var connectionInfo = new MySqlConnectionStringBuilder(connectionString);
            newConnection.ExecuteNonQuery($"CREATE DATABASE {IdentityName(connectionInfo.Database)};");
            return Task.CompletedTask;
        }

        public Task DropDatabaseIfExists(string connectionString)
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
            return Task.CompletedTask;
        }

      



        private static MySqlConnection CreateNoDatabaseConnection(string connection)
        {
            var builder = new MySqlConnectionStringBuilder(connection) { Database = "sys" };
            var newConnection = new MySqlConnection(builder.ConnectionString);
            return newConnection;
        }
    }
}
