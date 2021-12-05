using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb("dumpsql", HelpText = "Dump database as sql scripts")]
    internal class DumpSql : ICommand
    {

        [Value(1, MetaName = "source-dbtype", Required = true, HelpText = "source database type.")]
        public string SourceType { get; set; }

        [Value(2, MetaName = "source-connection", Required = true, HelpText = "source database connection strings")]
        public string SourceConnectionString { get; set; }

        [Value(3, MetaName = "target-dbtype", Required = false, HelpText = "target database type, default use source database type.")]
        public string TargetType { get; set; }

        [Option('f', "force", HelpText = "drop target sql scripts file1 if exists", Default = false)]
        public bool DropTargetDatabaseIfExists { get; set; } = false;

        [Option("schema-only", HelpText = "only dump schema (true/false)", Default = false)]
        public bool SchemaOnly { get; set; } = false;


        [Option("max-fetch-bytes", HelpText = "max fetch bytes when read source database (unit KB), default value is 10 (10KB).")]
        public int MaxFetchBytes { get; set; } = 10;
        public int Run()
        {
            throw new NotImplementedException();
        }
    }
}
