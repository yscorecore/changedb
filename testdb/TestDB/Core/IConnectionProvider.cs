using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface IConnectionProvider
    {
        string ChangeDatabase(string connectionString, string databaseName);

        string MakeReadOnly(string connectionString);

        DbConnection CreateConnection(string connectionString);
    }
}
