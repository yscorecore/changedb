namespace ChangeDB.Migration
{
    public class MigrationContext
    {
        public DatabaseInfo SourceDatabase { get; set; }
        public DatabaseInfo TargetDatabase { get; set; }
        public MigrationSetting Setting { get; set; }

        
    }
}
