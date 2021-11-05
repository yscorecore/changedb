using System.Data;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDataMigrator
    {
        Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, DatabaseInfo databaseInfo, MigrationSetting migrationSetting);

        Task<long> CountTable(TableDescriptor table, DatabaseInfo databaseInfo, MigrationSetting migrationSetting);

        Task WriteTableData(DataTable data, TableDescriptor table, DatabaseInfo databaseInfo, MigrationSetting migrationSetting);

    }
}
