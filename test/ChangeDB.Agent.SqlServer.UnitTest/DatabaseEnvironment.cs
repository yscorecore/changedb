using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
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
            var connectionTemplate = Environment.GetEnvironmentVariable("SqlServerConnectionString");
            if (string.IsNullOrEmpty(connectionTemplate))
            {
                var dbPort = Utility.GetRandomTcpPort();
                DockerCompose.Up(new Dictionary<string, object> { ["SQLSERVER_DBPORT"] = dbPort }, "db:1433");
                return ($"Server=127.0.0.1,{dbPort};User Id=sa;Password=myStrong(!)Password;", false);
            }
            else
            {
                return (connectionTemplate, true);
            }
        }

        private readonly Lazy<DbConnection> defaultConnectionFactory;
        private readonly string connectionTemplate;
        private readonly bool fromEnvironment;
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
            var builder = new SqlConnectionStringBuilder(this.connectionTemplate)
            {
                InitialCatalog = Utility.RandomDatabaseName()
            };
            return new SqlConnection(builder.ConnectionString);

        }
        private DbConnection CreateNewDatabase()
        {
            var conn = NewDatabaseConnection();
            conn.CreateDatabase();
            return conn;
        }
    }
}
