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
            DBPort = Utility.GetRandomTcpPort();
            DockerCompose.Up(new Dictionary<string, object> { ["DBPORT"] = DBPort }, "db:1433");

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
            return new SqlConnection(
                $"Server=127.0.0.1,{DBPort};Database={Utility.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;");
        }
    }
}
