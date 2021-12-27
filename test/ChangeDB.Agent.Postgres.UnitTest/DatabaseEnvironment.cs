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
            DBPort = Utility.GetRandomTcpPort();
            DockerCompose.Up(new Dictionary<string, object> { ["DBPORT"] = DBPort }, "db:5432");

            DbConnection = NewDatabaseConnection();
            DbConnection.CreateDatabase();
        }

        public int DBPort { get; }
        public DbConnection DbConnection { get; }

        public void Dispose()
        {
            DockerCompose.Down();
        }

        public DbConnection NewDatabaseConnection()
        {
            return new NpgsqlConnection(
                $"Server=127.0.0.1;Port={DBPort};Database={Utility.RandomDatabaseName()};User Id=postgres;Password=mypassword;");
        }
    }
}
