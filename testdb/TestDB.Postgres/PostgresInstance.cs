using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB.Postgres
{
    public class PostgresInstance : IDatabaseInstance
    {
        public PostgresInstance() : this("TESTDB_POSTGRES")
        {
        }
        public PostgresInstance(string envName)
        {
            this.ConnectionTemplate = Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(ConnectionTemplate))
            {
                var port = Utility.GetRandomTcpPort();
                dockerContainer = DockerCompose.Up(Path.Combine("dockerfiles", "postgres.yml"), new Dictionary<string, object> { ["POSTGRES_DBPORT"] = port }, "db:5432");
                this.ConnectionTemplate = $"Server=127.0.0.1;Port={port};User Id=postgres;Password=mypassword;";
                Environment.SetEnvironmentVariable(envName, this.ConnectionTemplate);
            }
        }
        public string ConnectionTemplate { get; }

        private readonly IDisposable dockerContainer;

        public void Dispose()
        {
            if (dockerContainer != null)
            {
                dockerContainer.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }
    }
}
