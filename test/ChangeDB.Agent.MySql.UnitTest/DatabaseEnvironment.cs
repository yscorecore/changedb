using System;
using System.Data.Common;
using MySqlConnector;
using Xunit;

namespace ChangeDB.Agent.MySql
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        public DatabaseEnvironment()
        {
            connectionTemplate = "Server=127.0.0.1;Uid=root;Pwd=password;";
            defaultConnectionFactory = new Lazy<DbConnection>(CreateNewDatabase);
        }
        private string connectionTemplate;

        private readonly Lazy<DbConnection> defaultConnectionFactory;
        public DbConnection DbConnection => defaultConnectionFactory.Value;


        public string NewConnectionString(string database)
        {
            var builder = new MySqlConnectionStringBuilder(connectionTemplate)
            {
                Database = database
            };
            return builder.ToString();
        }

        public DbConnection NewDatabaseConnection()
        {
            return new MySqlConnection(NewConnectionString(Utility.RandomDatabaseName()));
        }
        public DbConnection CreateNewDatabase()
        {
            var conn = NewDatabaseConnection();
            conn.CreateDatabase();
            return conn;
        }


        public void Dispose()
        {
            if (defaultConnectionFactory.IsValueCreated)
            {
                defaultConnectionFactory.Value.Dispose();
            }
        }
    }
}