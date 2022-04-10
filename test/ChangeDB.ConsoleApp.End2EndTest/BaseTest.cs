using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    [Collection(nameof(TestDatabaseEnvironment))]
    public class BaseTest
    {
        public BaseTest(TestDatabaseEnvironment testDatabaseEnvironment)
        {
            TestDatabaseEnvironment = testDatabaseEnvironment;
        }

        public TestDatabaseEnvironment TestDatabaseEnvironment { get; }

        public ITestDatabase CreateDatabase(string dbType, params string[] initsqls)
        {
            var testdatabaseManager = TestDatabaseEnvironment.DatabaseManagers[dbType];
            return testdatabaseManager.CreateDatabase(initsqls);
        }
        public ITestDatabase CreateDatabaseFromFile(string dbType, string fileName, string splitLine = "")
        {
            var testdatabaseManager = TestDatabaseEnvironment.DatabaseManagers[dbType];
            var testDatabase = testdatabaseManager.CreateDatabase();
            testDatabase.Connection.ExecuteSqlScriptFile(fileName, splitLine);
            return testDatabase;
        }


        public ITestDatabase RequestDatabase(string dbType)
        {
            var testdatabaseManager = TestDatabaseEnvironment.DatabaseManagers[dbType];
            return testdatabaseManager.RequestDatabase();
        }


        protected static (int, string, string) RunChangeDbApp(string arguments)
        {
            return Shell.ExecOrDebug("dotnet", $"ChangeDB.ConsoleApp.dll {arguments}");
        }
        protected static (int, string, string) RunChangeDbMigration(params string[] arguments)
        {
            return RunChangeDbApp($"migration {string.Join(' ', arguments.Select(EncodeArgument))}");
        }
        protected static (int, string, string) RunChangeDumpSql(string arguments)
        {
            return RunChangeDbApp($"migration {arguments}");
        }
        protected static (int, string, string) RunChangeImportSql(string arguments)
        {
            return RunChangeDbApp($"migration {arguments}");
        }
        private static string EncodeArgument(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return args;
            }
            return $"\"{args.Replace("\"", "\"\"")}\""; 
        }
    }
}
