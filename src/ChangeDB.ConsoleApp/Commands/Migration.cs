﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("migration", HelpText = "Migration database from source to target")]
    public class Migration : ICommand
    {
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

        [Option("schema-only", HelpText = "only migrate schema (true/false)")]
        public bool SchemaOnly { get; set; } = false;

        [Option("table-name-style", HelpText = "target table name style (Original/Lower/Upper).")]
        public NameStyle TableNameStyle { get; set; } = NameStyle.Original;
        public int Run()
        {
            var serviceHost = ServiceHost.Default;
            var serviceProvider = serviceHost.ServiceCollection.BuildServiceProvider();
            var service = serviceProvider.GetService<IDatabaseMigrate>();
            var task = service.MigrateDatabase(new MigrationContext
            {
                Setting = new MigrationSetting()
                {
                    MigrationType = SchemaOnly ? MigrationType.MetaData : MigrationType.All,
                    DropTargetDatabaseIfExists = DropTargetDatabaseIfExists,
                    TargetNameStyle = new TargetNameStyle
                    {
                        TableNameStyle = TableNameStyle
                    }
                },
                SourceDatabase = new DatabaseInfo { DatabaseType =  SourceType, ConnectionString = SourceConnectionString },
                TargetDatabase = new DatabaseInfo { DatabaseType = TargetType, ConnectionString = TargetConnectionString }
            });
            task.Wait();
            return 0;
        }
    }
}
