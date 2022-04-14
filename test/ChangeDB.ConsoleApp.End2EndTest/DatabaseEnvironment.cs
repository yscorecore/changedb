using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    [CollectionDefinition(nameof(TestDatabaseEnvironment))]
    public class TestDatabaseEnvironment : IDisposable, ICollectionFixture<TestDatabaseEnvironment>
    {
        public readonly static string[] SupportedDatabases = new[]
        {
            TestDatabases.POSTGRES,
            TestDatabases.SQLSERVER,
            TestDatabases.SQLCE,
            TestDatabases.MYSQL
        };
        public TestDatabaseEnvironment()
        {
            databases = TestDatabases.SetupEnvironments(SupportedDatabases);
            DatabaseManagers = TestDatabases.CreateManagersFromEnvironment(false);
        }
        private IDisposable[] databases = Array.Empty<IDisposable>();
        public IDictionary<string, ITestDatabaseManager> DatabaseManagers { get; } = new Dictionary<string, ITestDatabaseManager>(StringComparer.InvariantCultureIgnoreCase);

        public void Dispose()
        {
            Parallel.ForEach(DatabaseManagers.Values, (m) => m.Dispose());
            Parallel.ForEach(databases, m => m.Dispose());
        }
    }
}
