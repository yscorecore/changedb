using System;
using System.Diagnostics;
using System.Linq;
using CommandLine;

namespace ChangeDB.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            StartDebugger();
            var commands = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(p => p.IsClass && typeof(ICommand).IsAssignableFrom(p)).ToArray();
            var parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; with.HelpWriter = Console.Out; });
            return parser.ParseArguments(args, commands).MapResult(command => (command as ICommand).Run(), errors => 1);
        }
        [Conditional("DEBUG")]
        static void StartDebugger()
        {
            if (!Debugger.IsAttached && bool.TryParse(Environment.GetEnvironmentVariable("DEBUGING"), out var isDebuging) && isDebuging)
            {
                Debugger.Launch();
            }
        }
    }
}
