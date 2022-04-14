using System;
using System.Data;

namespace ChangeDB
{
    public interface ITestDatabase : IDisposable, IAsyncDisposable
    {
        IDbConnection Connection { get; }
        string ConnectionString { get; }
        string DatabaseName { get; }
        string ScriptSplit { get; }
    }
}
