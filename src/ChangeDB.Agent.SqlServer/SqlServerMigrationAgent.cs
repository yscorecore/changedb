using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator { get => SqlServerDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => SqlServerMetadataMigrator.Default; }
    }
}
