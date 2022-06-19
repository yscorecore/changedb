using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB.Sqlite
{
    public class SqliteInstance : IDatabaseInstance
    {
        public SqliteInstance() : this("TESTDB_SQLITE")
        {

        }
        public SqliteInstance(string envName)
        {
            this.ConnectionTemplate = Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Machine)
                ?? $"Data Source=sqlite.db;";
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
