using System.Collections.Generic;
using ChangeDB.Migration;

namespace ChangeDB.Import
{
    public class ImportContext
    {
        public DatabaseInfo TargetDatabase { get; init; }
        public CustomSqlScripts SqlScripts { get; set; }
        public bool ReCreateTargetDatabase { get; set; } = false;

    }
}
