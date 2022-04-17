using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestDB;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    public static class End2EndUtils
    {
        public static (int, string, string) RunChangeDbApp(string arguments)
        {
            return Shell.ExecOrDebug("dotnet", $"ChangeDB.ConsoleApp.dll {arguments}");
        }
        public static (int, string, string) RunChangeDbMigration(params string[] arguments)
        {
            return RunChangeDbApp($"migration {string.Join(' ', arguments.Select(EncodeArgument))}");
        }
        public static (int, string, string) RunChangeDumpSql(params string[] arguments)
        {
            return RunChangeDbApp($"dumpsql {string.Join(' ', arguments.Select(EncodeArgument))}");
        }

        public static (int, string, string) RunChangeImportSql(params string[] arguments)
        {
            return RunChangeDbApp($"importsql {string.Join(' ', arguments.Select(EncodeArgument))}");
        }
        public static string EncodeArgument(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return args;
            }
            return $"\"{args.Replace("\"", "\"\"")}\"";
        }
    }
}
