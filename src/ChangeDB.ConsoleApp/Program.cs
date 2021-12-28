using System.Linq;
using CommandLine;

namespace ChangeDB.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            var commands = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(p => p.IsClass && typeof(ICommand).IsAssignableFrom(p)).ToArray();
            var parser = new Parser(with => with.CaseInsensitiveEnumValues = true);
            return parser.ParseArguments(args, commands).MapResult(command => (command as ICommand).Run(), errors => 1);
        }
    }
}
