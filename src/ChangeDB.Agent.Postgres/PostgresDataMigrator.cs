using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataMigrator : IDataMigrator
    {
        public static readonly PostgresDataMigrator Default = new PostgresDataMigrator();


        public Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, DatabaseInfo databaseInfo,
            MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> CountTable(TableDescriptor table, DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            var sql = $"select count(1) from \"{table.Schema}\".\"{table.Name}\"";
            var val = databaseInfo.Connection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public Task WriteTableData(DataTable data, TableDescriptor table, DatabaseInfo databaseInfo,
            MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }
    }
}
