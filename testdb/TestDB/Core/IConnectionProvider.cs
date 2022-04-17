using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface IConnectionProvider
    {
        string ChangeDatabase(string connectionString, string databaseName);

        string MakeReadOnly(string connectionString);

        IDbConnection CreateConnection(string connectionString);
    }
}
