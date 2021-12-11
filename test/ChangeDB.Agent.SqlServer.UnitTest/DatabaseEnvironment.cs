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
            DBPort = Utility.GetAvailableTcpPort(1433);
            DockerCompose.Up(new Dictionary<string, object>
            {
                ["DBPORT"] = DBPort
            }, "db:1433");

            DbConnection = NewDatabaseConnection();
            DbConnection.CreateDatabase();
        }

        public uint DBPort { get; set; }
        public DbConnection DbConnection { get; }

        public DbConnection NewDatabaseConnection()
        {
            return new SqlConnection($"Server=127.0.0.1,1433;Database={Utility.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;");
        }

        public void Dispose()
        {
            DockerCompose.Down();
        }




    }
}
