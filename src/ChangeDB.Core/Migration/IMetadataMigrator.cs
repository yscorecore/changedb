using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IMetadataMigrator
    {
        Task<DatabaseDescriptor> GetSourceDatabaseDescriptor(MigrationContext migrationContext);

        Task PreMigrateTargetMetadata(DatabaseDescriptor databaseDescriptor, MigrationContext migrationContext);

        Task PostMigrateTargetMetadata(DatabaseDescriptor databaseDescriptor, MigrationContext migrationContext);
        public async Task MigrateAllTargetMetaData(DatabaseDescriptor databaseDescriptor, MigrationContext migrationContext)
        {
            await PreMigrateTargetMetadata(databaseDescriptor, migrationContext);
            await PostMigrateTargetMetadata(databaseDescriptor, migrationContext);
        }
    }
}
