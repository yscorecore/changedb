using System;
using ChangeDB.Import;
using ChangeDB.Migration;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("importsql", HelpText = "import sql script to target database")]
    public class ImportSql : BaseCommand
    {
        public override string CommandName { get => "importsql"; }
        [Value(1, MetaName = "Target-DBType", Required = true, HelpText = "Enter the type of Target database. (ej:mysql/postgres/sqlserver/sqlce)")]
        public string TargetType { get; set; }

        [Value(2, MetaName = "Target-DBConnection", Required = true, HelpText = "Enter the source database connection strings, you can get help from ChangeDB Readme page")]
        public string TargetConnectionString { get; set; }

        [Value(3, MetaName = "Extra-ExecuteFile", Required = true, HelpText = "Choose the sql file location, ChangeDB will execute choosen script after the migration.")]
        public string TargetScriptFile { get; set; }

        [Option("Scripts-Separator", Required = false, HelpText = "Enter the separator for transformed script, default seperator is \"\"", Default = "")]
        public string SqlSplit { get; set; } = string.Empty;

        [Option('r', "Recreate-Database", HelpText = "Setiing this option true ChangeDB will recreate new database, default value is FALSE", Default = false)]
        public bool ReCreateTargetDatabase { get; set; } = false;

        protected override void OnRunCommand()
        {
            var service = ServiceHost.Default.GetRequiredService<IDatabaseSqlImporter>();
            var context = this.BuildImportContext();
            service.Import(context).Wait();
        }

        private ImportContext BuildImportContext()
        {
            var context = new ImportContext
            {
                TargetDatabase = new DatabaseInfo
                {
                    DatabaseType = TargetType,
                    ConnectionString = TargetConnectionString
                },
                SqlScripts = new CustomSqlScript()
                {
                    SqlFile = TargetScriptFile,
                    SqlSplit = SqlSplit,
                },
                ReCreateTargetDatabase = ReCreateTargetDatabase
            };

            context.ObjectCreated += (sender, e) =>
            {
                Console.WriteLine(string.IsNullOrEmpty(e.OwnerName)
                    ? $"{e.ObjectType} {e.FullName} created."
                    : $"{e.ObjectType} {e.FullName} on {e.OwnerName} created.");
            };
            context.SqlExecuted += (sender, e) =>
            {
                Console.WriteLine($"Execute sql file line {e.StartLine:d3}...{e.StartLine + e.LineCount - 1:d3} success, return value {e.Result}.");
            };
            return context;
        }
    }
}
