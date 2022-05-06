using System;
using System.IO;
using ChangeDB.Dump;
using ChangeDB.Migration;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("dumpsql", HelpText = "Dump source database as target database sql scripts")]
    internal class DumpSql : BaseCommand
    {
        public override string CommandName { get => "dumpsql"; }

        [Value(1, MetaName = "source-dbtype", Required = true,
            HelpText = "Enter the type of input database. (ej:mysql/postgres/sqlserver/sqlce)")]
        public string SourceType { get; set; }

        [Value(2, MetaName = "source-dbconnection", Required = true,
            HelpText = "Enter the source database connection strings, you can get help from ChangeDB Readme page")]
        public string SourceConnectionString { get; set; }

        [Value(3, MetaName = "target-dbtype", Required = true,
            HelpText =
                "Enter the type of output database, ChangeDB will same database type as INPUT if output DBType isn't specified. (ej:mysql/postgres/sqlserver/sqlce)")]
        public string TargetType { get; set; }

        [Value(4, MetaName = "output-filepath", Required = true,
            HelpText = "Enter the file path to saved transformed sql scripts")]
        public string TargetScript { get; set; }

        [Option('f', "force", HelpText = "Warning:This option will DROP target sql script file if exists in path",
            Default = false)]
        public bool DropTargetDatabaseIfExists { get; set; } = false;

        [Option("dump-scope",
            HelpText = "Identify which part of database you want to transform. (ej:All/Metadata/Data)",
            Default = MigrationScope.All)]
        public MigrationScope DumpScope { get; set; } = MigrationScope.All;

        [Option("name-style",
            HelpText = "Identify the naming style of transformed database scripts. (ej:Original/Lower/Upper).")]
        public NameStyle NameStyle { get; set; } = NameStyle.Original;

        [Option("max-fetch-bytes",
            HelpText =
                "Enter the max capacity when ChangeDB fetch data from source database, default value is 100 (100 here equals 100KB).")]
        public int MaxFetchBytes { get; set; } = 100;

        [Option("post-sql-file",
            HelpText = "Choose the sql file location, ChangeDB will execute choosen script after the migration.")]
        public string PostSqlFile { get; set; }

        [Option("post-sql-file-split", Required = false,
            HelpText = "Enter the separator for transformed script, default seperator is \"\"", Default = "")]
        public string PostSqlSplit { get; set; } = "";


        [Option("target-default-schema", HelpText = "target database default schema.")]
        public string TargetDefaultSchema { get; set; }

        [Option("optimize-insertion",
            HelpText = "Set ture changedb sill optimize insertion script of transformed sql scripts.", Default = true)]
        public bool OptimizeInsertion { get; set; } = true;


        [Option("hide-progress",
            HelpText =
                "Set ture console table will hide transfrom indicator while ChangeDB migrate database. default value is false.",
            Default = false)]
        public bool HideProgress { get; set; }


        protected override void OnRunCommand()
        {
            var service = ServiceHost.Default.GetRequiredService<IDatabaseSqlDumper>();
            var fileMode = this.DropTargetDatabaseIfExists ? FileMode.Create : FileMode.CreateNew;
            using var fileStream = File.Open(TargetScript, fileMode, FileAccess.Write);
            using var writer = new StreamWriter(fileStream);
            var dumpInfo = this.BuildDumpInfo(writer);
            var eventReporter = new EventReporter(CanShowProgress());
            service.DumpSql(dumpInfo, eventReporter).Wait();
            writer.Flush();
        }

        private DumpSetting BuildDumpInfo(TextWriter textWriter)
        {
            return new DumpSetting
            {
                MigrationScope = DumpScope,
                DropTargetDatabaseIfExists = DropTargetDatabaseIfExists,
                TargetNameStyle = new TargetNameStyle { NameStyle = NameStyle },
                FetchDataMaxSize = MaxFetchBytes * 1024,
                PostScript = new CustomSqlScript() { SqlFile = PostSqlFile, SqlSplit = PostSqlSplit, },
                TargetDefaultSchema = TargetDefaultSchema,

                SourceDatabase =
                    new DatabaseInfo { DatabaseType = SourceType, ConnectionString = SourceConnectionString },
                TargetDatabase = new DatabaseInfo { DatabaseType = TargetType, ConnectionString = string.Empty },
                Writer = textWriter
            };
        }

        private bool CanShowProgress()
        {
            return !HideProgress && !Console.IsOutputRedirected;
        }

        class EventReporter : IEventReporter
        {
            private readonly ConsoleProgressBarManager _consoleProgressBarManager;

            public EventReporter(bool showProgressBar)
            {
                _consoleProgressBarManager = showProgressBar ? new ConsoleProgressBarManager() : default;
            }

            public void RaiseEvent<T>(T eventInfo)
                where T : IEventInfo
            {
                switch (eventInfo)
                {
                    case ObjectInfo objectInfo:
                        ShowObjectInfo(objectInfo);
                        break;
                    case TableDataInfo tableDataInfo:
                        ShowTableDataInfo(tableDataInfo);
                        break;
                    case StageInfo stageInfo:
                        ShowStageInfo(stageInfo);
                        break;
                    default:
                        Console.WriteLine(eventInfo);
                        break;
                }
            }

            private void ShowStageInfo(StageInfo stageInfo)
            {
                if (_consoleProgressBarManager != null)
                {
                    if (stageInfo.Stage == StageKind.StartingTableData)
                    {
                        _consoleProgressBarManager.Start();
                    }
                    else if (stageInfo.Stage == StageKind.FinishedTableData)
                    {
                        _consoleProgressBarManager.End();
                    }
                }
            }

            private void ShowObjectInfo(ObjectInfo objectInfo)
            {
                Console.WriteLine(string.IsNullOrEmpty(objectInfo.OwnerName)
                    ? $"{objectInfo.ObjectType} {objectInfo.FullName} dumped."
                    : $"{objectInfo.ObjectType} {objectInfo.FullName} on {objectInfo.OwnerName} dumped.");
            }

            private void ShowTableDataInfo(TableDataInfo e)
            {
                if (_consoleProgressBarManager == null)
                {
                    if (e.Completed)
                    {
                        Console.WriteLine($"Data of table {e.Table} dumped.");
                    }
                }
                else
                {
                    _consoleProgressBarManager.ReportProgress(e.Table,
                        e.Completed ? $"Data of table {e.Table} dumped." : $"Dumping data of table {e.Table}"
                        , e.TotalCount, e.MigratedCount, e.Completed);
                }
            }
        }
    }
}
