﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDataMigrator : IDataMigrator
    {
        public static readonly IDataMigrator Default = new SqlServerDataMigrator();
        private static HashSet<string> canNotOrderByTypes = new HashSet<string>() { "image", "text", "ntext", "xml" };
        private static string BuildTableName(TableDescriptor table) => SqlServerUtils.IdentityName(table.Schema, table.Name);
        private static string BuildColumnNames(IEnumerable<string> names) => string.Join(", ", names.Select(p => $"[{p}]"));
        private static string BuildColumnNames(TableDescriptor table) =>
            BuildColumnNames(table.Columns.Select(p => p.Name));
        private string BuildParameterValueNames(TableDescriptor table) => string.Join(", ", table.Columns.Select(p => $"@{p.Name}"));
        private static string BuildOrderByColumnNames(TableDescriptor table)
        {
            if (table.PrimaryKey?.Columns?.Count > 0)
            {
                return BuildColumnNames(table.PrimaryKey?.Columns.ToArray());
            }

            var names = table.Columns.Where(p => !canNotOrderByTypes.Contains(p.StoreType.ToLower())).Select(p => p.Name);

            return BuildColumnNames(names);
        }

        public Task<long> CountSourceTable(TableDescriptor table, MigrationContext migrationContext)
        {
            var sql = $"select count_big(1) from {BuildTableName(table)}";
            var val = migrationContext.SourceConnection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, MigrationContext migrationContext)
        {
            var sql =
                $"select * from {BuildTableName(table)} order by {BuildOrderByColumnNames(table)} offset {pageInfo.Offset} row fetch next {pageInfo.Limit} row only";
            return Task.FromResult(migrationContext.SourceConnection.ExecuteReaderAsTable(sql));
        }

        public Task WriteTargetTable(DataTable data, TableDescriptor table, MigrationContext migrationContext)
        {
            if (table.Columns.Count == 0)
            {
                return Task.CompletedTask;
            }
            var insertSql = $"INSERT INTO {BuildTableName(table)}({BuildColumnNames(table)}) VALUES ({BuildParameterValueNames(table)});";
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
            var tableFullName = SqlServerUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
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
                var tableFullName = SqlServerUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
                migrationContext.TargetConnection.ExecuteNonQuery($"SET IDENTITY_INSERT {tableFullName} OFF");

                tableDescriptor.Columns.Where(p => p.IdentityInfo?.CurrentValue != null)
                    .Each((column) =>
                    {
                        migrationContext.TargetConnection.ExecuteNonQuery($"DBCC CHECKIDENT ('{tableFullName}', RESEED, {column.IdentityInfo.CurrentValue})");
                    });
            }

            return Task.CompletedTask;
        }
    }
}
