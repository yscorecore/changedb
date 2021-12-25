using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.SqlCe.SqlCeUtils;
namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDataMigrator : IDataMigrator
    {
        public static readonly IDataMigrator Default = new SqlCeDataMigrator();
        private static string BuildColumnNames(IEnumerable<string> names) => string.Join(", ", names.Select(p => $"[{p}]"));
        private static string BuildColumnNames(TableDescriptor table) =>
            BuildColumnNames(table.Columns.Select(p => p.Name));
        private string BuildParameterValueNames(TableDescriptor table) => string.Join(", ", table.Columns.Select(p => $"@{p.Name}"));
        private static string BuildPrimaryKeyColumnNames(TableDescriptor table)
        {
            if (table.PrimaryKey?.Columns?.Count > 0)
            {
                return BuildColumnNames(table.PrimaryKey?.Columns.ToArray());
            }
            return BuildColumnNames(table);
        }

        public Task<long> CountSourceTable(TableDescriptor table, MigrationContext migrationContext)
        {
            var sql = $"select count(1) from {IdentityName(table)}";
            var val = migrationContext.SourceConnection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, MigrationContext migrationContext)
        {
            var sql =
                $"select * from {IdentityName(table)} order by {BuildPrimaryKeyColumnNames(table)} offset {pageInfo.Offset} row fetch next {pageInfo.Limit} row only";
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

        private IDictionary<string, object> GetRowData(DataRow row, TableDescriptor tableDescriptor)
        {
            var dic = new Dictionary<string, object>();
            foreach (var column in tableDescriptor.Columns)
            {
                dic[$"@{column.Name}"] = row[column.Name];
            }
            return dic;
        }


        public Task BeforeWriteTargetTable(TableDescriptor tableDescriptor, MigrationContext migrationContext)
        {
            var tableFullName = SqlCeUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
            if (tableDescriptor.Columns.Any(p => p.IdentityInfo != null))
            {
                migrationContext.TargetConnection.ExecuteNonQuery($"SET IDENTITY_INSERT {tableFullName} ON");

            }

            return Task.CompletedTask;
        }

        public Task AfterWriteTargetTable(TableDescriptor tableDescriptor, MigrationContext migrationContext)
        {
            if (tableDescriptor.Columns.Any(p => p.IdentityInfo != null))
            {
                var tableFullName = SqlCeUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
                migrationContext.TargetConnection.ExecuteNonQuery($"SET IDENTITY_INSERT {tableFullName} OFF");

                tableDescriptor.Columns.Where(p => p.IdentityInfo?.CurrentValue != null)
                    .Each((column) =>
                    {
                        var startValue = column.IdentityInfo.CurrentValue + column.IdentityInfo.IncrementBy;
                        var incrementBy = column.IdentityInfo.IncrementBy;
                        migrationContext.TargetConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {SqlCeUtils.IdentityName(column.Name)} IDENTITY({startValue},{incrementBy})");
                    });
            }

            return Task.CompletedTask;
        }
    }
}
