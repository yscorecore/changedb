namespace ChangeDB.Migration
{
    public class MigrationSetting
    {
        public bool IncludeMeta { get; set; } = true;
        public bool IncludeData { get; set; } = true;

        public int MaxPageSize { get; set; } = 10000;
    }
}
