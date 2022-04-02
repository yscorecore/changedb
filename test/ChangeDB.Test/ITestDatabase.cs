using System;
using System.Data;
using System.Threading.Tasks;

namespace ChangeDB
{
    public interface ITestDatabase: IDisposable,IAsyncDisposable
    {
        IDbConnection Connection { get; }
        string ConnectionString { get; }
        string DatabaseName { get; }
    }
}
