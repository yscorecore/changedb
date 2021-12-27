using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Loader;
using ChangeDB.Migration;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("migration", HelpText = "Migration database from source to target")]
    public class Migration : BaseCommand
    {
        public override string CommandName { get => "migration"; }
        [Value(1, MetaName = "source-dbtype", Required = true, HelpText = "source database type.")]
        public string SourceType { get; set; }

        [Value(2, MetaName = "source-connection", Required = true, HelpText = "source database connection strings")]
        public string SourceConnectionString { get; set; }

        [Value(3, MetaName = "target-dbtype", Required = true, HelpText = "target database type.")]
        public string TargetType { get; set; }

        [Value(4, MetaName = "target-connection", Required = true, HelpText = "target database connection strings")]
        public string TargetConnectionString { get; set; }


        [Option('f', "force", HelpText = "drop target database if exists")]
        public bool DropTargetDatabaseIfExists { get; set; } = false;



        [Option("migration-scope", HelpText = "(All/Metadata/Data)", Default = MigrationScope.All)]
        public MigrationScope MigrationScope { get; set; } = MigrationScope.All;

        [Option("name-style", HelpText = "target object name style (Original/Lower/Upper).")]
        public NameStyle NameStyle { get; set; } = NameStyle.Original;

        [Option("max-fetch-bytes", HelpText = "max fetch bytes when read source database (unit KB), default value is 100 (100KB).")]
        public int MaxFetchBytes { get; set; } = 100;

        [Option("post-sql-file",
            HelpText = "post sql file, execute these sql script after the migration one-by-one.")]
        public string PostSqlFile { get; set; }

        [Option("post-sql-file-split", HelpText = "sql file split chars, default value is \"\"")]
        public string PostSqlSplit { get; set; } = string.Empty;



        protected override void OnRunCommand()
        {
            var service = ServiceHost.Default.GetRequiredService<IDatabaseMigrate>();
            var context = BuildMigrationContext();
            service.MigrateDatabase(context).Wait();
        }

        private MigrationContext BuildMigrationContext()
        {
            var context = new MigrationContext
            {
                Setting = new MigrationSetting()
                {
                    MigrationScope = MigrationScope,
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
                    }
                },

                SourceDatabase = new DatabaseInfo { DatabaseType = SourceType, ConnectionString = SourceConnectionString },
                TargetDatabase = new DatabaseInfo { DatabaseType = TargetType, ConnectionString = TargetConnectionString }
            };
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
            context.EventReporter.ObjectCreated += (sender, e) =>
            {
                Console.WriteLine(string.IsNullOrEmpty(e.OwnerName)
                    ? $"{e.ObjectType} {e.FullName} created."
                    : $"{e.ObjectType} {e.FullName} on {e.OwnerName} created.");
            };
            context.EventReporter.TableDataMigrated += (sender, e) =>
            {
                consoleProgressBarManager.ReportProgress(e.Table,
                    e.Completed ? $"Data of table {e.Table} migrated." : $"Migrating data of table {e.Table}"
                    , e.TotalCount, e.MigratedCount, e.Completed);
            };
            return context;
        }


    }
}
