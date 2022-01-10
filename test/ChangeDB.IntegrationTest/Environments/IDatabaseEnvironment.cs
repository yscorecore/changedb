using System;
using System.Data.Common;

namespace ChangeDB.Environments
{
    public interface IDatabaseEnvironment : IDisposable
    {
        string NewConnectionString();
        DbConnection CreateConnection(string connectionString);
    }
}
