using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerMetadataMigrator : IMetadataMigrator
    {
        public static readonly IMetadataMigrator Default = new SqlServerMetadataMigrator();

        public Task<DatabaseDescriptor> GetDatabaseDescriptor(DbConnection connection, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task PostMigrate(DatabaseDescriptor databaseDescriptor, DbConnection connection, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task PreMigrate(DatabaseDescriptor databaseDescriptor, DbConnection connection, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }
    }
}
