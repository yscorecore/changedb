using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerMetadataMigrator:IMetadataMigrator
    {
        public static readonly IMetadataMigrator Default = new SqlServerMetadataMigrator();
        public Task<DatabaseDescriptor> GetDatabaseDescriptor(DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task PreMigrate(DatabaseDescriptor databaseDescriptor, DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task PostMigrate(DatabaseDescriptor databaseDescriptor, DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task PreMigrate(DatabaseDescriptor databaseDescriptor, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task PostMigrate(DatabaseDescriptor databaseDescriptor, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }


    }
}
