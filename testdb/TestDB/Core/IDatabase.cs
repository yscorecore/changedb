using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface IDatabase : IDisposable, IAsyncDisposable
    {
        IDbConnection Connection { get; }
        string ConnectionString { get; }
        string DatabaseName { get; }
    }
}
