using ChangeDB.Migration;

namespace ChangeDB.Import
{
    public record ImportSetting
    {
        public DatabaseInfo TargetDatabase { get; init; }
        public CustomSqlScript SqlScripts { get; init; }
        public bool ReCreateTargetDatabase { get; init; }
    }
}
