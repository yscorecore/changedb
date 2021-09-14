using System.Data;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDataMigrator
    {
        Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, MigrationSetting migrationSetting);

        Task<long> CountTable(TableDescriptor table, MigrationSetting migrationSetting);

        Task WriteTableData(DataTable data, TableDescriptor table, MigrationSetting migrationSetting);

    }
}
