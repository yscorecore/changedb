using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Migration
{
    public interface ITableDataMapper
    {
        Task<DataTable> MapDataTable(DataTable dataTable, TableDescriptorMapper tableDescriptorMapper,
            MigrationSetting migrationSetting);
    }
}
