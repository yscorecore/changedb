using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Environments
{
    public class SqlServerEnvironment : IDatabaseEnvironment
    {
        private const string DockerComposeFile = "Environments/docker-compose-sqlserver.yml";
        public SqlServerEnvironment()
        {
            connectionTemplate = Environment.GetEnvironmentVariable("SqlServerConnectionString");
            if (string.IsNullOrEmpty(connectionTemplate))
            {
                fromEnvironment = false;
                var dbPort = Utility.GetRandomTcpPort();
                DockerCompose.Up(DockerComposeFile, new Dictionary<string, object> { ["SQLSERVER_DBPORT"] = dbPort }, "db:1433");
                connectionTemplate = $"Server=127.0.0.1,{dbPort};User Id=sa;Password=myStrong(!)Password;";
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
            new SqlConnectionStringBuilder(this.connectionTemplate)
            {
                InitialCatalog = Utility.RandomDatabaseName()
            }.ConnectionString;

        public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);
    }
}
