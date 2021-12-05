using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("dumpsql", HelpText = "Dump database as sql scripts")]
    internal class DumpSql : ICommand
    {

        [Value(1, MetaName = "source-dbtype", Required = true, HelpText = "source database type.")]
        public string SourceType { get; set; }

        [Value(2, MetaName = "source-connection", Required = true, HelpText = "source database connection strings")]
        public string SourceConnectionString { get; set; }

        [Value(3, MetaName = "target-dbtype", Required = true, HelpText = "target database type, default use source database type.")]
        public string TargetType { get; set; }


        [Value(4, MetaName = "output-file", Required = true, HelpText = "output script file")]
        public string TargetScript { get; set; }

        [Option('f', "force", HelpText = "drop target sql scripts file1 if exists", Default = false)]
        public bool DropTargetDatabaseIfExists { get; set; } = false;

        [Option("metadata-only", HelpText = "only dump metadata (true/false)", Default = false)]
        public bool MetadataOnly { get; set; } = false;

        [Option("name-style", HelpText = "target object name style (Original/Lower/Upper).")]
        public NameStyle NameStyle { get; set; } = NameStyle.Original;
        [Option("max-fetch-bytes", HelpText = "max fetch bytes when read source database (unit KB), default value is 10 (10KB).")]
        public int MaxFetchBytes { get; set; } = 10;

        [Option("post-sql-file",
          HelpText = "post sql file, execute these sql script after the migration one-by-one.")]
        public string PostSqlFile { get; set; }

        [Option("post-sql-file-split", HelpText = "sql file split chars, default value is ;;")]
        public string PostSqlSplit { get; set; } = ";;";
        public int Run()
        {
            var serviceHost = ServiceHost.Default;
            var serviceProvider = serviceHost.ServiceCollection.BuildServiceProvider();
            var service = serviceProvider.GetService<IDatabaseSqlDumper>();
            var task = service.DumpSql(new DumpContext
            {
                Setting = new MigrationSetting()
                {
                    MigrationType = MetadataOnly ? MigrationType.MetaData : MigrationType.All,
                    DropTargetDatabaseIfExists = DropTargetDatabaseIfExists,
                    TargetNameStyle = new TargetNameStyle
                    {
                        NameStyle = NameStyle
                    },
                    FetchDataMaxSize = MaxFetchBytes * 1024,
                    PostScripts = new CustomSqlScripts()
                    {
                        SqlFiles = string.IsNullOrEmpty(PostSqlFile) ? new List<string>() : new List<string>() { PostSqlFile },
                        SqlSplit = PostSqlSplit,
                    },


                },
                SourceDatabase = new DatabaseInfo { DatabaseType = SourceType, ConnectionString = SourceConnectionString },
                DumpInfo = new SqlScriptInfo { DatabaseType = TargetType, SqlScriptFile = TargetScript }
            });
            task.Wait();
            return 0;
        }
    }
}
