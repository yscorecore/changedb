using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IMetadataMigrator
    {
        Task<DatabaseDescriptor> GetDatabaseDescriptor(DbConnection connection, MigrationContext migrationContext);

        Task PreMigrate(DatabaseDescriptor databaseDescriptor, DbConnection connection, MigrationContext migrationContext);

        Task PostMigrate(DatabaseDescriptor databaseDescriptor, DbConnection connection, MigrationContext migrationContext);
        public async Task MigrateAllMetaData(DatabaseDescriptor databaseDescriptor, DbConnection connection, MigrationContext migrationContext)
        {
            await PreMigrate(databaseDescriptor, connection, migrationContext);
            await PostMigrate(databaseDescriptor, connection, migrationContext);
        }
    }
}
