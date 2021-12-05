using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Dump
{
    public class DumpContext
    {
        public DatabaseInfo SourceDatabase { get; set; }
        public SqlScriptInfo DumpInfo { get; set; }
        public MigrationSetting Setting { get; set; }
    }
}
