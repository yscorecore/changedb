using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDataMigrator : IDataMigrator
    {
        public static readonly SqlServerDataMigrator Default = new SqlServerDataMigrator();

        public Task<long> CountTable(TableDescriptor table, DbConnection connection, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, DbConnection connection, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task WriteTableData(DataTable data, TableDescriptor table, DbConnection connection, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }
    }
}
