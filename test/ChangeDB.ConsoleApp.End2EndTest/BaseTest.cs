using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestDB;
using Xunit;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    [Collection(nameof(DatabaseEnvironment))]
    public class BaseTest
    {

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
