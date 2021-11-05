using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDataMigrator : IDataMigrator
    {
        public static readonly SqlServerDataMigrator Default = new SqlServerDataMigrator();
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

        public Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, DatabaseInfo databaseInfo,
            MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> CountTable(TableDescriptor table, DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task WriteTableData(DataTable data, TableDescriptor table, DatabaseInfo databaseInfo,
            MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }
    }
}
