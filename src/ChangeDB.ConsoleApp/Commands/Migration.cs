using System;
using ChangeDB.Migration;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("migration", HelpText = "Migration database from source to target")]
    public class Migration : BaseCommand
    {
        public override string CommandName { get => "migration"; }
        [Value(1, MetaName = "source-dbtype", Required = true, HelpText = "Enter the type of Source dtabase. (ej:mysql/postgres/sqlserver/sqlce)")]
        public string SourceType { get; set; }

        [Value(2, MetaName = "source-dbconnection", Required = true, HelpText = "Enter the source database connection strings, you can get help from ChangeDB Readme page")]
        public string SourceConnectionString { get; set; }

        [Value(3, MetaName = "target-dbtype", Required = true, HelpText = "Enter the type of Target database. (ej:mysql/postgres/sqlserver/sqlce)")]
        public string TargetType { get; set; }

        [Value(4, MetaName = "target-dbconnection", Required = true, HelpText = "Enter the target database connection strings, you can get help from ChangeDB Readme page")]
        public string TargetConnectionString { get; set; }


        [Option('f', "force", HelpText = "Warning:This option will DROP target sql script file if exists in path")]
        public bool DropTargetDatabaseIfExists { get; set; } = false;

        [Option("migration-scope", HelpText = "Identify which part of database you want to migration. (ej:All/Metadata/Data)(All/Metadata/Data)", Default = MigrationScope.All)]
        public MigrationScope MigrationScope { get; set; } = MigrationScope.All;

        [Option("name-style", HelpText = "Identify the naming style of transformed database scripts. (ej:Original/Lower/Upper).")]
        public NameStyle NameStyle { get; set; } = NameStyle.Original;

        [Option("max-fetch-bytes", HelpText = "Enter the max capacity when ChangeDB fetch data from source database, default value is 100 (100 here equals 100KB).")]
        public int MaxFetchBytes { get; set; } = 100;

        [Option("post-sql-file", HelpText = "Choose the sql file location, ChangeDB will execute choosen script after the migration.")]
        public string PostSqlFile { get; set; }

        [Option("post-sql-file-split", Required = false, HelpText = "Enter the separator for transformed script, default seperator is \"\"", Default = "")]
        public string PostSqlSplit { get; set; } = string.Empty;

        [Option("target-default-schema", HelpText = "target database default schema.")]
        public string TargetDefaultSchema { get; set; }


        [Option("hide-progress", HelpText = "Set ture console table will hide transfrom indicator while ChangeDB migrate database. default value is false.", Default = false)]
        public bool HideProgress { get; set; }


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
                    },
                    TargetDefaultSchema = TargetDefaultSchema
                },

                SourceDatabase = new DatabaseInfo { DatabaseType = SourceType, ConnectionString = SourceConnectionString },
                TargetDatabase = new DatabaseInfo { DatabaseType = TargetType, ConnectionString = TargetConnectionString }
            };
            ShowConsoleMessage(context);
            return context;
        }
        private bool CanShowProgress()
        {
            return !HideProgress && !Console.IsOutputRedirected;
        }
        private void ShowConsoleMessage(MigrationContext context)
        {
            context.EventReporter.ObjectCreated += (sender, e) =>
            {
                Console.WriteLine(string.IsNullOrEmpty(e.OwnerName)
                    ? $"{e.ObjectType} {e.FullName} created."
                    : $"{e.ObjectType} {e.FullName} on {e.OwnerName} created.");
            };

            if (CanShowProgress())
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
                        e.Completed ? $"Data of table {e.Table} migrated." : $"Migrating data of table {e.Table}"
                        , e.TotalCount, e.MigratedCount, e.Completed);
                };
            }
            else
            {
                context.EventReporter.TableDataMigrated += (sender, e) =>
                {
                    if (e.Completed)
                    {
                        Console.WriteLine($"Data of table {e.Table} migrated.");
                    }
                };
            }


        }
    }
}
