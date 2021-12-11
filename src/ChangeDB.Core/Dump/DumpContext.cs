using ChangeDB.Migration;

namespace ChangeDB.Dump
{
    public record DumpContext : MigrationContext
    {
        public SqlScriptInfo DumpInfo { get; set; }
    }
}
