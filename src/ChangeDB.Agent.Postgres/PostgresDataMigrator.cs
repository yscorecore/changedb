using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataMigrator:IDataMigrator
    {
        public static readonly PostgresDataMigrator Default = new PostgresDataMigrator();
        public Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> CountTable(TableDescriptor table, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task WriteTableData(DataTable data, TableDescriptor table, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }
    }
}
