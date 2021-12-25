using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataMigrator : IDataMigrator
    {
        public static readonly PostgresDataMigrator Default = new PostgresDataMigrator();

        public Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, MigrationContext migrationContext)
        {
            var sql = $"select * from {IdentityName(table)} limit {pageInfo.Limit} offset {pageInfo.Offset}";
            return Task.FromResult(migrationContext.SourceConnection.ExecuteReaderAsTable(sql));
        }

        public Task<long> CountSourceTable(TableDescriptor table, MigrationContext migrationContext)
        {
            var sql = $"select count(1) from {IdentityName(table)}";
            var totalCount = migrationContext.SourceConnection.ExecuteScalar<long>(sql);
            return Task.FromResult(totalCount);
        }

        public Task WriteTargetTable(DataTable data, TableDescriptor table, MigrationContext migrationContext)
        {
            if (table.Columns.Count == 0 || data.Rows.Count == 0)
            {
                return Task.CompletedTask;
            }
            var overIdentity = OverIdentityType(table);
            var insertSql = $"INSERT INTO {IdentityName(table)}({BuildColumnNames(table)}) {overIdentity} VALUES ({BuildParameterValueNames(table)});";
            foreach (DataRow row in data.Rows)
            {
                var rowData = GetRowData(row, table);
                migrationContext.TargetConnection.ExecuteNonQuery(insertSql, rowData);
            }

            return Task.CompletedTask;

            string OverIdentityType(TableDescriptor tableDescriptor)
            {
                var identityInfo = tableDescriptor.Columns.Where(p => p.IsIdentity && p.IdentityInfo != null).Select(p => p.IdentityInfo).SingleOrDefault();
                if (identityInfo == null)
                {
                    return string.Empty;
                }
                var identityType = PostgresUtils.IDENTITY_ALWAYS;

                if (identityInfo.Values != null && identityInfo.Values.TryGetValue(PostgresUtils.IdentityType, out var type))
                {
                    identityType = Convert.ToString(type);
                }
                return identityType switch
                {
                    PostgresUtils.IDENTITY_ALWAYS => "OVERRIDING SYSTEM VALUE",
                    PostgresUtils.IDENTITY_BYDEFAULT => "OVERRIDING USER VALUE",
                    _ => string.Empty
                };
            }
        }



        private string BuildColumnNames(TableDescriptor table)
        {
            return string.Join(", ", table.Columns.Select(p => $"{IdentityName(p.Name)}"));
        }

        private string BuildParameterValueNames(TableDescriptor table)
        {
            return string.Join(", ", table.Columns.Select(p => $"@{p.Name}"));
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
            return Task.CompletedTask;
        }

        public Task AfterWriteTargetTable(TableDescriptor tableDescriptor, MigrationContext migrationContext)
        {
            var tableFullName = PostgresUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
            tableDescriptor.Columns.Where(p => p.IdentityInfo?.CurrentValue != null)
                .Each((column) =>
                {
                    migrationContext.TargetConnection.ExecuteNonQuery($"SELECT setval(pg_get_serial_sequence('{tableFullName}','{column.Name}'),{column.IdentityInfo.CurrentValue})");
                });
            return Task.CompletedTask;
        }

        private string IdentityName(string schema, string objectName) => PostgresUtils.IdentityName(schema, objectName);

        private string IdentityName(TableDescriptor table) => IdentityName(table.Schema, table.Name);

        private string IdentityName(string objectName) => PostgresUtils.IdentityName(objectName);
    }
}
