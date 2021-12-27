using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using Xunit;

namespace ChangeDB
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        public DatabaseEnvironment()
        {
            SqlServerPort = Utility.GetRandomTcpPort();
            PostgresPort = Utility.GetRandomTcpPort();
            DockerCompose.Up(
                new Dictionary<string, object> { ["POSTGRES_PORT"] = PostgresPort, ["SQLSERVER_PORT"] = SqlServerPort },
                new Dictionary<string, int> { ["postgres"] = 5432, ["sqlserver"] = 1433 });
        }

        public int SqlServerPort { get; }
        public int PostgresPort { get; }

        public void Dispose()
        {
            DockerCompose.Down();
        }


        public DbConnection NewPostgresConnection()
        {
            return new NpgsqlConnection(
                $"Server=127.0.0.1;Port={PostgresPort};Database={Utility.RandomDatabaseName()};User Id=postgres;Password=mypassword;");
        }

        public DbConnection NewSqlServerConnection()
        {
            return new SqlConnection(
                $"Server=127.0.0.1,{SqlServerPort};Database={Utility.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;");
        }
    }
}
