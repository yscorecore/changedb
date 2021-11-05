using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IMetadataMigrator
    {
        Task<DatabaseDescriptor> GetDatabaseDescriptor(DatabaseInfo databaseInfo, MigrationSetting migrationSetting);

        Task PreMigrate(DatabaseDescriptor databaseDescriptor, DatabaseInfo databaseInfo, MigrationSetting migrationSetting);

        Task PostMigrate(DatabaseDescriptor databaseDescriptor, DatabaseInfo databaseInfo, MigrationSetting migrationSetting);
    }
}
