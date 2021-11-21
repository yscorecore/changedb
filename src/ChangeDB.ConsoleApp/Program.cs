using System;
using System.Linq;
using CommandLine;

namespace ChangeDB.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            PluginRegister.LoadPlugins();
            var commands = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(p => p.IsClass && typeof(ICommand).IsAssignableFrom(p)).ToArray();
            return Parser.Default.ParseArguments(args, commands).MapResult(command => (command as ICommand).Run(), errors => 1);
        }
    }
}
