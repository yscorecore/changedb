using System;
using System.Collections.Generic;
using System.Data.Common;
using Npgsql;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        public DatabaseEnvironment()
        {
            (connectionTemplate, fromEnvironment) = GetConnectionString();
            defaultConnectionFactory = new Lazy<DbConnection>(CreateNewDatabase);
        }

        private static (string Conn, bool FromEnv) GetConnectionString()
        {
            var connectionTemplate = Environment.GetEnvironmentVariable("PostgresConnectionString");
            if (string.IsNullOrEmpty(connectionTemplate))
            {
                var dbPort = Utility.GetRandomTcpPort();
                DockerCompose.Up(new Dictionary<string, object> { ["POSTGRES_DBPORT"] = dbPort }, "db:5432");
                return ($"Server=localhost;Port={dbPort};User Id=postgres;Password=mypassword;", false);
            }
            else
            {
                return (connectionTemplate, true);
            }
        }

        private Lazy<DbConnection> defaultConnectionFactory;
        private string connectionTemplate;
        private bool fromEnvironment;
        public DbConnection DbConnection { get => defaultConnectionFactory.Value; }

        public void Dispose()
        {
            if (!fromEnvironment)
            {
                DockerCompose.Down();
            }
        }
        public DbConnection NewDatabaseConnection()
        {
            var builder = new NpgsqlConnectionStringBuilder(this.connectionTemplate)
            {
                Database = Utility.RandomDatabaseName()
            };
            return new NpgsqlConnection(builder.ConnectionString);

        }
        private DbConnection CreateNewDatabase()
        {
            var conn = NewDatabaseConnection();
            conn.CreateDatabase();
            return conn;
        }
    }
}
