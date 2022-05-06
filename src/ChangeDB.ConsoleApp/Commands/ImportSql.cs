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
        [Value(1, MetaName = "target-dbtype", Required = true, HelpText = "Enter the type of Target database. (ej:mysql/postgres/sqlserver/sqlce)")]
        public string TargetType { get; set; }

        [Value(2, MetaName = "target-dbconnection", Required = true, HelpText = "Enter the source database connection strings, you can get help from ChangeDB Readme page")]
        public string TargetConnectionString { get; set; }

        [Value(3, MetaName = "import-file", Required = true, HelpText = "Choose the sql file location, ChangeDB will execute choosen script after the migration.")]
        public string ScriptFile { get; set; }

        [Option("sql-file-split", Required = false, HelpText = "Enter the separator for transformed script, default seperator is \"\"", Default = "")]
        public string SqlSplit { get; set; } = string.Empty;

        [Option('r', "recreate-new", HelpText = "Setting this option true ChangeDB will recreate new database, default value is FALSE", Default = false)]
        public bool ReCreateTargetDatabase { get; set; } = false;

        protected override void OnRunCommand()
        {
            var service = ServiceHost.Default.GetRequiredService<IDatabaseSqlImporter>();
            var info = this.BuildImportInfo();
            var eventReporter = new EventReporter();
            service.Import(info, eventReporter).Wait();
        }

        private ImportSetting BuildImportInfo()
        {
            return new()
            {
                TargetDatabase = new DatabaseInfo
                {
                    DatabaseType = TargetType,
                    ConnectionString = TargetConnectionString
                },
                SqlScripts = new CustomSqlScript()
                {
                    SqlFile = ScriptFile,
                    SqlSplit = SqlSplit,
                },
                ReCreateTargetDatabase = ReCreateTargetDatabase
            };
        }

        class EventReporter : IEventReporter
        {
            public void RaiseEvent<T>(T eventInfo)
                where T : IEventInfo
            {
                switch (eventInfo)
                {
                    case ObjectInfo objectInfo:
                        Console.WriteLine(string.IsNullOrEmpty(objectInfo.OwnerName)
                            ? $"{objectInfo.ObjectType} {objectInfo.FullName} created."
                            : $"{objectInfo.ObjectType} {objectInfo.FullName} on {objectInfo.OwnerName} created.");
                        break;
                    case SqlSegmentInfo sqlSegmentInfo:
                        Console.WriteLine(
                            $"Execute sql file line {sqlSegmentInfo.StartLine:d3}...{sqlSegmentInfo.StartLine + sqlSegmentInfo.LineCount - 1:d3} success, return value {sqlSegmentInfo.Result}.");
                        break;
                    default:
                        Console.WriteLine(eventInfo);
                        break;
                }
            }
        }
    }


}
