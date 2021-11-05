using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresMigrationAgent: IMigrationAgent
    {

        public IDataMigrator DataMigrator { get=>PostgresDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => PostgresMetadataMigrator.Default; }
    }
}
