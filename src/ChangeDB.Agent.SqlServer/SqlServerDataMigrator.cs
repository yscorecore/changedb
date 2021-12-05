using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDataMigrator : IDataMigrator
    {
        public static readonly IDataMigrator Default = new SqlServerDataMigrator();
        private static string BuildTableName(TableDescriptor table) => SqlServerUtils.IdentityName(table.Schema, table.Name);
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

        public Task<long> CountTable(TableDescriptor table, DbConnection connection, MigrationSetting migrationSetting)
        {
            var sql = $"select count(1) from {BuildTableName(table)}";
            var val = connection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, DbConnection connection, MigrationSetting migrationSetting)
        {
            var sql =
                $"select * from {BuildTableName(table)} order by {BuildPrimaryKeyColumnNames(table)} offset {pageInfo.Offset} row fetch next {pageInfo.Limit} row only";
            return Task.FromResult(connection.ExecuteReaderAsTable(sql));
        }

        public Task WriteTableData(DataTable data, TableDescriptor table, DbConnection connection, MigrationSetting migrationSetting)
        {
            if (table.Columns.Count == 0)
            {
                return Task.CompletedTask;
            }
            var insertSql = $"INSERT INTO {BuildTableName(table)}({BuildColumnNames(table)}) VALUES ({BuildParameterValueNames(table)});";
            foreach (DataRow row in data.Rows)
            {
                var rowData = GetRowData(row, table);
                connection.ExecuteNonQuery(insertSql, rowData);
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


        public Task BeforeWriteTableData(TableDescriptor tableDescriptor, DbConnection connection, MigrationSetting migrationSetting)
        {
            var tableFullName = SqlServerUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
            if (tableDescriptor.Columns.Any(p => p.IdentityInfo != null))
            {
                connection.ExecuteNonQuery($"SET IDENTITY_INSERT {tableFullName} ON");

            }

            return Task.CompletedTask;
        }

        public Task AfterWriteTableData(TableDescriptor tableDescriptor, DbConnection connection, MigrationSetting migrationSetting)
        {
            if (tableDescriptor.Columns.Any(p => p.IdentityInfo != null))
            {
                var tableFullName = SqlServerUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
                connection.ExecuteNonQuery($"SET IDENTITY_INSERT {tableFullName} OFF");

                tableDescriptor.Columns.Where(p => p.IdentityInfo?.CurrentValue != null)
                    .Each((column) =>
                    {
                        connection.ExecuteNonQuery($"DBCC CHECKIDENT ('{tableFullName}', RESEED, {column.IdentityInfo.CurrentValue})");
                    });
            }

            return Task.CompletedTask;
        }
    }
}
