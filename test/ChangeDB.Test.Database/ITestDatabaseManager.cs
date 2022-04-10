using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public interface ITestDatabaseManager : IDisposable,IAsyncDisposable
    {
        ITestDatabase CreateDatabase();
        ITestDatabase RequestDatabase();
    }


    public static class TestDatabaseManagerExtensions
    {
        public static ITestDatabase CreateDatabase(this ITestDatabaseManager databaseManager, IEnumerable<string> initSqls)
        {
            var database = databaseManager.CreateDatabase();
            foreach (var sql in initSqls ?? Enumerable.Empty<string>())
            {
                _ = database.Connection.ExecuteNonQuery(sql);
            }
            return database;
        }
    }
}
