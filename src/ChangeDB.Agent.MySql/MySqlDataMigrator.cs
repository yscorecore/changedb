using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.MySql.MySqlUtils;

namespace ChangeDB.Agent.MySql
{
    public class MySqlDataMigrator : IDataMigrator
    {
        public static readonly IDataMigrator Default = new MySqlDataMigrator();
        private static string BuildColumnNames(IEnumerable<string> names) => string.Join(", ", names.Select(IdentityName));
        private static string BuildColumnNames(TableDescriptor table) =>
            BuildColumnNames(table.Columns.Select(p => p.Name));
        private string BuildParameterValueNames(TableDescriptor table) => string.Join(", ", table.Columns.Select(p => $"@{p.Name}"));

        public Task<long> CountSourceTable(TableDescriptor table, MigrationContext migrationContext)
        {
            var sql = $"select count(1) from {IdentityName(table)}";
            var val = migrationContext.SourceConnection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, MigrationContext migrationContext)
        {
            var sql =
                $"select * from {IdentityName(table)} limit {pageInfo.Limit} offset {pageInfo.Offset}";
            return Task.FromResult(migrationContext.SourceConnection.ExecuteReaderAsTable(sql));
        }

        public Task WriteTargetTable(DataTable data, TableDescriptor table, MigrationContext migrationContext)
        {
            if (table.Columns.Count == 0)
            {
                return Task.CompletedTask;
            }
            var insertSql = $"INSERT INTO {IdentityName(table)}({BuildColumnNames(table)}) VALUES ({BuildParameterValueNames(table)});";
            foreach (DataRow row in data.Rows)
            {
                var rowData = GetRowData(row, table);
                migrationContext.TargetConnection.ExecuteNonQuery(insertSql, rowData);
            }
            return Task.CompletedTask;
        }

        private static IDictionary<string, object> GetRowData(DataRow row, TableDescriptor tableDescriptor)
        {
            return tableDescriptor.Columns.ToDictionary(p => $"@{p.Name}", p => row[p.Name]);
        }


        public Task BeforeWriteTargetTable(TableDescriptor tableDescriptor, MigrationContext migrationContext)
        {
            return Task.CompletedTask;
        }

        public Task AfterWriteTargetTable(TableDescriptor tableDescriptor, MigrationContext migrationContext)
        {
            var identityColumn = tableDescriptor.Columns.FirstOrDefault(p => p.IsIdentity && p.IdentityInfo?.CurrentValue != null);
            if (identityColumn != null)
            {
                var tableFullName = IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
                migrationContext.TargetConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} AUTO_INCREMENT = {identityColumn.IdentityInfo.CurrentValue + identityColumn.IdentityInfo.IncrementBy }");
            }
            return Task.CompletedTask;
        }
    }
}
