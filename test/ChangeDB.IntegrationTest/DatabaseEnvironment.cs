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
            //DockerCompose.Down();
        }




        public string NewConnectionString(string dbType)
        {
            return dbType?.ToLowerInvariant() switch
            {
                "sqlserver" => $"Server=127.0.0.1,{SqlServerPort};Database={Utility.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;",
                "postgres" => $"Server=127.0.0.1;Port={PostgresPort};Database={Utility.RandomDatabaseName()};User Id=postgres;Password=mypassword;",
                _ => throw new NotSupportedException($"not support database type {dbType}")
            };
        }

        public DbConnection NewConnection(string dbType)
        {
            return NewConnection(dbType, NewConnectionString(dbType));
        }
        public DbConnection NewConnection(string dbType, string connectionString)
        {
            return dbType?.ToLowerInvariant() switch
            {
                "sqlserver" => new SqlConnection(connectionString),
                "postgres" => new NpgsqlConnection(connectionString),
                _ => throw new NotSupportedException($"not support database type {dbType}")
            };
        }
    }
}
