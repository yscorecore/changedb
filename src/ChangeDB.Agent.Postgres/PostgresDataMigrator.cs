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
            var sql = $"select * from \"{table.Schema}\".\"{table.Name}\" limit {pageInfo.Limit} offset {pageInfo.Offset}";
            return Task.FromResult(databaseInfo.Connection.ExecuteReaderAsTable(sql));
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
