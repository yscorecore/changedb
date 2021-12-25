using CommandLine;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("import-sql", HelpText = "import sql script to target database")]
    public class ImportSql : ICommand
    {
        [Value(1, MetaName = "target-dbtype", Required = true, HelpText = "target database type.")]
        public string TargetType { get; set; }

        [Value(2, MetaName = "target-connection", Required = true, HelpText = "target database connection strings")]
        public string TargetConnectionString { get; set; }

        [Value(4, MetaName = "script-file", Required = true, HelpText = "the script file path")]
        public string TargetScriptFile { get; set; }

        public int Run()
        {
            return 0;
        }
    }
}
