using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB.MySql
{
    public class MySqlInstance : IDatabaseInstance
    {
        public MySqlInstance() : this("TESTDB_MYSQL")
        {

        }
        public MySqlInstance(string envName)
        {
            this.ConnectionTemplate = Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(ConnectionTemplate))
            {
                var port = Utility.GetRandomTcpPort();
                dockerContainer = DockerCompose.Up(Path.Combine("dockerfiles", "mysql.yml"), new Dictionary<string, object> { ["MySQL_DBPORT"] = port }, "db:3306");
                this.ConnectionTemplate = $"Server=127.0.0.1;Port={port};Uid=root;Pwd=password;";
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
