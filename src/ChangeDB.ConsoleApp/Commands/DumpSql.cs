using System;
using System.Collections.Generic;
using ChangeDB.Dump;
using ChangeDB.Migration;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("dumpsql", HelpText = "Dump database as sql scripts")]
    internal class DumpSql : BaseCommand
    {
        public override string CommandName { get => "dumpsql"; }

        [Value(1, MetaName = "source-dbtype", Required = true, HelpText = "source database type. (mysql/postgres/sqlserver/sqlce)")]
        public string SourceType { get; set; }

        [Value(2, MetaName = "source-connection", Required = true, HelpText = "source database connection strings")]
        public string SourceConnectionString { get; set; }

        [Value(3, MetaName = "target-dbtype", Required = true, HelpText = "target database type, default use source database type. (mysql/postgres/sqlserver/sqlce)")]
        public string TargetType { get; set; }


        [Value(4, MetaName = "output-file", Required = true, HelpText = "output script file")]
        public string TargetScript { get; set; }

        [Option('f', "force", HelpText = "drop target sql script file if exists", Default = false)]
        public bool DropTargetDatabaseIfExists { get; set; } = false;

        [Option("dump-scope", HelpText = "(All/Metadata/Data)", Default = MigrationScope.All)]
        public MigrationScope DumpScope { get; set; } = MigrationScope.All;

        [Option("name-style", HelpText = "target object name style (Original/Lower/Upper).")]
        public NameStyle NameStyle { get; set; } = NameStyle.Original;
        [Option("max-fetch-bytes", HelpText = "max fetch bytes when read source database (unit KB), default value is 10 (100KB).")]
        public int MaxFetchBytes { get; set; } = 100;


        [Option("pre-sql-file",
  HelpText = "pre sql file, execute these sql script before the migration one-by-one.", Separator = ',')]
        public IEnumerable<string> PreSqlFile { get; set; }

        [Option("pre-sql-file-split", Required = false, HelpText = "pre sql file split chars, default value is \"\"", Default = "")]
        public string PreSqlSplit { get; set; } = "";

        [Option("post-sql-file",
          HelpText = "post sql file, execute these sql script after the migration one-by-one.", Separator = ',')]
        public IEnumerable<string> PostSqlFile { get; set; }

        [Option("post-sql-file-split", Required = false, HelpText = "post sql file split chars, default value is \"\"", Default = "")]
        public string PostSqlSplit { get; set; } = "";


        [Option("target-default-schema",
            HelpText = "target database default schema.")]
        public string TargetDefaultSchema { get; set; }

        [Option("optimize-insertion",
            HelpText = "optimize insertion script.", Default = true)]
        public bool OptimizeInsertion { get; set; } = true;


        [Option("hide-progress",
            HelpText = "hide progress bar.", Default = false)]
        public bool HideProgress { get; set; }
        [Option("tables", HelpText = "the tables to include. if empty, include all tables.", Separator = ',')]
        public IEnumerable<string> Tables { get; set; }
        [Option("schemas", HelpText = "the schemas to include. if empty, include all schemas.", Separator = ',')]
        public IEnumerable<string> Schemas { get; set; }

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
                    TargetNameStyle = new TargetNameStyle
                    {
                        NameStyle = NameStyle
                    },
                    FetchDataMaxSize = MaxFetchBytes * 1024,
                    PreScript = new CustomSqlScript()
                    {
                        SqlFile = PreSqlFile,
                        SqlSplit = PreSqlSplit,
                    },
                    PostScript = new CustomSqlScript()
                    {
                        SqlFile = PostSqlFile,
                        SqlSplit = PostSqlSplit,
                    },
                    TargetDefaultSchema = TargetDefaultSchema,
                    Filter = new Filter { Tables = Tables, Schemas = Schemas }

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
