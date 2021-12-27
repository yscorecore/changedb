using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using ChangeDB.Dump;
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
        [Value(1, MetaName = "target-dbtype", Required = true, HelpText = "target database type.")]
        public string TargetType { get; set; }

        [Value(2, MetaName = "target-connection", Required = true, HelpText = "target database connection strings")]
        public string TargetConnectionString { get; set; }

        [Value(3, MetaName = "script-file", Required = true, HelpText = "the script file path")]
        public string TargetScriptFile { get; set; }

        [Option("sql-file-split", Required = false, HelpText = "sql file split chars, default value is \"\"", Default = "")]
        public string SqlSplit { get; set; } = string.Empty;

        [Option('r', "recreate-new", HelpText = "recreate new database", Default = false)]
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
