using System;
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

        [Value(1, MetaName = "Source-DBType", Required = true, HelpText = "Enter the type of input database. (ej:mysql/postgres/sqlserver/sqlce)")]
        public string SourceType { get; set; }

        [Value(2, MetaName = "Source-DBConnection", Required = true, HelpText = "Enter the source database connection strings, you can get help from ChangeDB Readme page")]
        public string SourceConnectionString { get; set; }

        [Value(3, MetaName = "Target-DBType", Required = true, HelpText = "Enter the type of output database, ChangeDB will same database type as INPUT if output DBType isn't specified. (ej:mysql/postgres/sqlserver/sqlce)")]
        public string TargetType { get; set; }

        [Value(4, MetaName = "Output-FilePath", Required = true, HelpText = "Enter the file path to saved transformed sql scripts")]
        public string TargetScript { get; set; }

        [Option('f', "force", HelpText = "Warning:This option will DROP target sql script file if exists in path", Default = false)]
        public bool DropTargetDatabaseIfExists { get; set; } = false;

        [Option("Dump-Option", HelpText = "Identify which part of database you want to transform. (ej:All/Metadata/Data)", Default = MigrationScope.All)]
        public MigrationScope DumpScope { get; set; } = MigrationScope.All;

        [Option("Naming-Style", HelpText = "Identify the naming style of transformed database scripts. (ej:Original/Lower/Upper).")]
        public NameStyle NameStyle { get; set; } = NameStyle.Original;

        [Option("Max-Fetch-Capacity", HelpText = "Enter the max capacity when ChangeDB fetch data from source database, default value is 100 (100 here equals 100KB).")]
        public int MaxFetchBytes { get; set; } = 100;

        [Option("Extra-ExecuteFile", HelpText = "Choose the sql file location, ChangeDB will execute choosen script after the migration.")]
        public string PostSqlFile { get; set; }

        [Option("Scripts-Separator", Required = false, HelpText = "Enter the separator for transformed script, default seperator is \"\"", Default = "")]
        public string PostSqlSplit { get; set; } = "";


        [Option("Target-Default-Schema", HelpText = "target database default schema.")]
        public string TargetDefaultSchema { get; set; }

        [Option("Optimize-Scripts-Insertion", HelpText = "Set ture changedb sill optimize insertion script of transformed sql scripts.", Default = true)]
        public bool OptimizeInsertion { get; set; } = true;


        [Option("Hide-Process-Indicator", HelpText = "Set ture console table will hide transfrom indicator while ChangeDB migrate database. default value is false.", Default = false)]
        public bool HideProgress { get; set; }



        protected override void OnRunCommand()
        {
            var service = ServiceHost.Default.GetRequiredService<IDatabaseSqlDumper>();
            var context = this.BuildDumpContext();
            service.DumpSql(context).Wait();
        }

        private DumpContext BuildDumpContext()
        {
            var context = new DumpContext
            {
                Setting = new MigrationSetting()
                {
                    MigrationScope = DumpScope,
                    DropTargetDatabaseIfExists = DropTargetDatabaseIfExists,
                    TargetNameStyle = new TargetNameStyle
                    {
                        NameStyle = NameStyle
                    },
                    FetchDataMaxSize = MaxFetchBytes * 1024,
                    PostScript = new CustomSqlScript()
                    {
                        SqlFile = PostSqlFile,
                        SqlSplit = PostSqlSplit,
                    },
                    TargetDefaultSchema = TargetDefaultSchema

                },

                SourceDatabase = new DatabaseInfo { DatabaseType = SourceType, ConnectionString = SourceConnectionString },
                TargetDatabase = new DatabaseInfo { DatabaseType = TargetType, ConnectionString = String.Empty },
                DumpInfo = new SqlScriptInfo { DatabaseType = TargetType, SqlScriptFile = TargetScript },
            };
            WriteConsoleMessage(context);
            return context;
        }

        private void WriteConsoleMessage(DumpContext context)
        {
            context.EventReporter.ObjectCreated += (sender, e) =>
            {
                Console.WriteLine(string.IsNullOrEmpty(e.OwnerName)
                    ? $"{e.ObjectType} {e.FullName} dumped."
                    : $"{e.ObjectType} {e.FullName} on {e.OwnerName} dumped.");
            };

            if (!HideProgress)
            {
                ConsoleProgressBarManager consoleProgressBarManager = new ConsoleProgressBarManager();
                context.EventReporter.StageChanged += (sender, e) =>
                {
                    if (e == StageKind.StartingTableData)
                    {
                        consoleProgressBarManager.Start();
                    }
                    else if (e == StageKind.FinishedTableData)
                    {
                        consoleProgressBarManager.End();
                    }
                };

                context.EventReporter.TableDataMigrated += (sender, e) =>
                {
                    consoleProgressBarManager.ReportProgress(e.Table,
                        e.Completed ? $"Data of table {e.Table} dumped." : $"Dumping data of table {e.Table}"
                        , e.TotalCount, e.MigratedCount, e.Completed);
                };
            }
            else
            {
                context.EventReporter.TableDataMigrated += (sender, e) =>
                {
                    if (e.Completed)
                    {
                        Console.WriteLine($"Data of table {e.Table} dumped.");
                    }
                };
            }

        }
    }
}
