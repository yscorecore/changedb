using System;
using System.Collections.Generic;
using System.Data.Common;
using Npgsql;
using Xunit;

namespace ChangeDB.Environments
{
    public class PostgresEnvironment : IDatabaseEnvironment
    {
        private const string DockerComposeFile = "Environments/docker-compose-postgres.yml";
        public PostgresEnvironment()
        {
            connectionTemplate = Environment.GetEnvironmentVariable("PostgresConnectionString");
            if (string.IsNullOrEmpty(connectionTemplate))
            {
                fromEnvironment = false;
                var dbPort = Utility.GetRandomTcpPort();
                DockerCompose.Up(DockerComposeFile, new Dictionary<string, object> { ["POSTGRES_DBPORT"] = dbPort }, "db:5432");
                connectionTemplate = $"Server=localhost;Port={dbPort};User Id=postgres;Password=mypassword;";
            }
            else
            {
                fromEnvironment = true;
            }

        }

        private readonly string connectionTemplate;
        private readonly bool fromEnvironment;

        public void Dispose()
        {
            if (!fromEnvironment)
            {
                DockerCompose.Down(DockerComposeFile);
            }
        }
        public string NewConnectionString() =>
            new NpgsqlConnectionStringBuilder(this.connectionTemplate)
            {
                Database = Utility.RandomDatabaseName()
            }.ConnectionString;

        public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);
    }
}
