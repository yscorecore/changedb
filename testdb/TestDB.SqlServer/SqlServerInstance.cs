using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB.SqlServer
{
    public class SqlServerInstance : IDatabaseInstance
    {
        public SqlServerInstance() : this("TESTDB_SQLSERVER")
        {

        }
        public SqlServerInstance(string envName)
        {
            this.ConnectionTemplate = Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(ConnectionTemplate))
            {
                var port = Utility.GetRandomTcpPort();
                dockerContainer = DockerCompose.Up(Path.Combine("dockerfiles", "sqlserver.yml"), new Dictionary<string, object> { ["SQLSERVER_DBPORT"] = port }, "db:1433");
                this.ConnectionTemplate = $"Server=127.0.0.1,{port};User Id=sa;Password=myStrong(!)Password;";
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
