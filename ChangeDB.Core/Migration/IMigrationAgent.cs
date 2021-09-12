using System.Data;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IMigrationAgent
    {
        Task<DatabaseDescriptor> GetDatabaseDescriptor(DatabaseInfo databaseInfo, MigrationSetting migrationSetting);

        Task<DataTable> FetchTableData(string tableName, PageInfo pageInfo, MigrationSetting migrationSetting);

        Task WriteTableData(DataTable dataTable, string tableName, MigrationSetting migrationSetting);
        
        Task PreMigrate(DatabaseDescriptor databaseDescriptor, MigrationSetting migrationSetting);

        Task PostTransfer(DatabaseDescriptor databaseDescriptor, MigrationSetting migrationSetting);
    }
}
