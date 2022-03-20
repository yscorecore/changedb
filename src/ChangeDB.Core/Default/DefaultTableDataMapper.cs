using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Default
{
    public class DefaultTableDataMapper : ITableDataMapper
    {
        public Task<DataTable> MapDataTable(DataTable dataTable, TableDescriptorMapper tableDescriptorMapper, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }
    }
}
