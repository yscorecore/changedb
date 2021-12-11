using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataMigrator : IDataMigrator
    {
        public static readonly PostgresDataMigrator Default = new PostgresDataMigrator();


        public Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, DbConnection dbConnection,
            MigrationContext migrationContext)
        {
            var sql = $"select * from {BuildTableName(table)} limit {pageInfo.Limit} offset {pageInfo.Offset}";
            return Task.FromResult(dbConnection.ExecuteReaderAsTable(sql));
        }

        public Task<long> CountTable(TableDescriptor table, DbConnection dbConnection, MigrationContext migrationContext)
        {
            var sql = $"select count(1) from {BuildTableName(table)}";
            var val = dbConnection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public Task WriteTableData(DataTable data, TableDescriptor table, DbConnection dbConnection,
            MigrationContext migrationContext)
        {
            if (table.Columns.Count == 0)
            {
                return Task.CompletedTask;
            }
            var overIdentity = OverIdentityType(table);
            var insertSql = $"INSERT INTO {BuildTableName(table)}({BuildColumnNames(table)}) {overIdentity} VALUES ({BuildParameterValueNames(table)});";
            foreach (DataRow row in data.Rows)
            {
                var rowData = GetRowData(row, table);
                dbConnection.ExecuteNonQuery(insertSql, rowData);
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

        private string BuildTableName(TableDescriptor table)
        {
            return $"\"{table.Schema}\".\"{table.Name}\"";
        }

        private string BuildColumnNames(TableDescriptor table)
        {
            return string.Join(", ", table.Columns.Select(p => $"\"{p.Name}\""));
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

        public Task BeforeWriteTableData(TableDescriptor tableDescriptor, DbConnection connection, MigrationContext migrationContext)
        {
            return Task.CompletedTask;
        }

        public Task AfterWriteTableData(TableDescriptor tableDescriptor, DbConnection connection, MigrationContext migrationContext)
        {
            var tableFullName = PostgresUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
            tableDescriptor.Columns.Where(p => p.IdentityInfo?.CurrentValue != null)
                .Each((column) =>
                {
                    connection.ExecuteNonQuery($"SELECT setval(pg_get_serial_sequence('{tableFullName}','{column.Name}'),{column.IdentityInfo.CurrentValue})");
                });
            return Task.CompletedTask;
        }
    }
}
