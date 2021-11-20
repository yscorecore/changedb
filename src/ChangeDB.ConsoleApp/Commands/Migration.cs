using System;
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

        public int Run()
        {
            var service = ServiceHost.Default.GetRequiredService<IDatabaseMigrate>();
            var task = service.MigrateDatabase(new MigrationContext
            {
                Setting = new MigrationSetting()
                {
                    DropTargetDatabaseIfExists = true,
                    MaxPageSize = 500,
                    // TargetNameStyle = new TargetNameStyle { NameStyle = NameStyle.Lower, TableNameStyle = NameStyle.Original }
                },
                SourceDatabase = new DatabaseInfo { Type = SourceType, ConnectionString = SourceConnectionString },
                TargetDatabase = new DatabaseInfo { Type = TargetType, ConnectionString = TargetConnectionString }
            });
            task.Wait();
            return 0;
        }
    }
}
