using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
            var sql = $"select * from {BuildTableName(table)} limit {pageInfo.Limit} offset {pageInfo.Offset}";
            return Task.FromResult(databaseInfo.Connection.ExecuteReaderAsTable(sql));
        }

        public Task<long> CountTable(TableDescriptor table, DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            var sql = $"select count(1) from {BuildTableName(table)}";
            var val = databaseInfo.Connection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public Task WriteTableData(DataTable data, TableDescriptor table, DatabaseInfo databaseInfo,
            MigrationSetting migrationSetting)
        {
            if (table.Columns.Count == 0)
            {
                return Task.CompletedTask;
            }

            var insertSql = $"insert into  {BuildTableName(table)}({BuildColumnNames(table)}) values ({BuildParameterValueNames(table)});";
            foreach (DataRow row in data.Rows)
            {
                var rowData = GetRowData(row, table);
                databaseInfo.Connection.ExecuteNonQuery(insertSql, rowData);
            }

            return Task.CompletedTask;
        }

        private string BuildTableName(TableDescriptor table)
        {
            return $"\"{table.Schema}\".\"{table.Name}\"";
        }

        private string BuildColumnNames(TableDescriptor table)
        {
            return string.Join(",", table.Columns.Select(p => $"\"{p.Name}\""));
        }

        private string BuildParameterValueNames(TableDescriptor table)
        {
            return string.Join(",", table.Columns.Select(p => $"@{p.Name}"));
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


    }
}
