using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            SqlServerPort = Utility.GetAvailableTcpPort(1433);
            PostgresPort = Utility.GetAvailableTcpPort(5432);
            DockerCompose.Up(new Dictionary<string, object>
            {
                ["POSTGRES_PORT"] = PostgresPort,
                ["SQLSERVER_PORT"] = SqlServerPort,
            }, new Dictionary<string, int>
            {
                ["postgres"] = 5432,
                ["sqlserver"] = 1433,
            });
        }

        public uint SqlServerPort { get; } = 1433;
        public uint PostgresPort { get; } = 5432;


        public DbConnection NewPostgresConnection()
        {
            return new NpgsqlConnection($"Server=127.0.0.1;Port={PostgresPort};Database={Utility.RandomDatabaseName()};User Id=postgres;Password=mypassword;");
        }
        public DbConnection NewSqlServerConnection()
        {
            return new SqlConnection($"Server=127.0.0.1,1433;Database={Utility.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;");
        }

        public void Dispose()
        {
             DockerCompose.Down();
        }
    }
}
