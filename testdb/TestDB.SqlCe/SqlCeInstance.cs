using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB.SqlCe
{
    public class SqlCeInstance : IDatabaseInstance
    {
        public SqlCeInstance() : this("TESTDB_SQLCE")
        {

        }
        public SqlCeInstance(string envName)
        {
            this.ConnectionTemplate = Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Machine)
                ?? $"Data Source=MyData.sdf;Max Database Size=4091;Persist Security Info=False;Persist Security Info=False;";
        }



        public string ConnectionTemplate { get; }

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }
    }
}
