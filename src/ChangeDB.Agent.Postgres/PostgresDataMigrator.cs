using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Npgsql;
using NpgsqlTypes;
using static ChangeDB.Agent.Postgres.PostgresUtils;

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

        public async Task WriteTargetTable(DataTable data, TableDescriptor table, MigrationContext migrationContext)
        {
            if (table.Columns.Count == 0 || data.Rows.Count == 0)
            {
                return;
            }

            if (migrationContext.MigrationType == MigrationType.SqlScript)
            {
                await InsertTable(data, table, migrationContext);
            }
            else
            {
                await BulkInsertTable(data, table, migrationContext);
            }
        }

        private Task InsertTable(DataTable data, TableDescriptor table, MigrationContext migrationContext)
        {
            var overIdentity = OverIdentityType(table);
            var insertSql = string.IsNullOrEmpty(overIdentity) ? $"INSERT INTO {IdentityName(table)}({BuildColumnNames(table)}) VALUES ({BuildParameterValueNames(table)});"
                    : $"INSERT INTO {IdentityName(table)}({BuildColumnNames(table)}) {overIdentity} VALUES ({BuildParameterValueNames(table)});";

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
                var identityType = IdentityAlways;

                if (identityInfo.Values != null && identityInfo.Values.TryGetValue(IdentityType, out var type))
                {
                    identityType = Convert.ToString(type);
                }
                return identityType switch
                {
                    IdentityAlways => "OVERRIDING SYSTEM VALUE",
                    IdentityByDefault => "OVERRIDING USER VALUE",
                    _ => string.Empty
                };
            }
        }

        private Task BulkInsertTable(DataTable data, TableDescriptor table, MigrationContext migrationContext)
        {
            var conn = migrationContext.TargetConnection as NpgsqlConnection;
            conn.TryOpen();
            using var writer = conn.BeginBinaryImport($"COPY {IdentityName(table)}({BuildColumnNames(table)}) FROM STDIN BINARY");

            var typeMapper = (from column in table.Columns
                              let normalizeStoreType = NormalizeStoreType(column.StoreType ?? string.Empty)
                              where NpgsqlTypeMapper.ContainsKey(normalizeStoreType)
                              select new { column.Name, NpgSqlType = NpgsqlTypeMapper[normalizeStoreType] })
                .ToDictionary(p => p.Name, p => p.NpgSqlType);


            foreach (DataRow row in data.Rows)
            {
                writer.StartRow();

                foreach (var column in table.Columns)
                {
                    var val = row[column.Name];
                    if (typeMapper.TryGetValue(column.Name, out var npgsqlDbType))
                    {
                        writer.Write(val, npgsqlDbType);
                    }
                    else
                    {
                        writer.Write(val);
                    }
                }
            }

            writer.Complete();
            return Task.CompletedTask;

        }

        private static string NormalizeStoreType(string storeType)
        {
            // abc(1,2) t
            var startIndex = storeType.IndexOf('(');
            var endIndex = storeType.LastIndexOf(')');
            if (startIndex > 0 && endIndex > startIndex)
            {
                return storeType[..startIndex].ToUpper() + storeType[(endIndex + 1)..].ToUpper();
            }
            return storeType.ToUpper();

        }

        private static readonly Dictionary<string, NpgsqlDbType> NpgsqlTypeMapper = new Dictionary<string, NpgsqlDbType>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["DATE"] = NpgsqlDbType.Date,
            ["TIME WITHOUT TIME ZONE"] = NpgsqlDbType.Time,
            ["TIME WITH TIME ZONE"] = NpgsqlDbType.TimeTz,
            ["TIMESTAMP WITHOUT TIME ZONE"] = NpgsqlDbType.Timestamp,
            ["TIMESTAMP WITH TIME ZONE"] = NpgsqlDbType.TimestampTz,
        };

        private string BuildColumnNames(TableDescriptor table) => string.Join(", ", table.Columns.Select(p => $"{IdentityName(p.Name)}"));

        private string BuildParameterValueNames(TableDescriptor table) => string.Join(", ", table.Columns.Select(p => $"@{p.Name}"));

        private IDictionary<string, object> GetRowData(DataRow row, TableDescriptor tableDescriptor)
        {
            var dic = new Dictionary<string, object>();
            tableDescriptor.Columns.Each(column => { dic[$"@{column.Name}"] = row[column.Name]; });
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
                    migrationContext.TargetConnection.ExecuteNonQuery($"SELECT SETVAL(PG_GET_SERIAL_SEQUENCE('{tableFullName}','{column.Name}'),{column.IdentityInfo.CurrentValue})");
                });
            return Task.CompletedTask;
        }

    }
}
