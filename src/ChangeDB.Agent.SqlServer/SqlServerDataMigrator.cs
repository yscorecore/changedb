using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;
using static ChangeDB.Agent.SqlServer.SqlServerUtils;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDataMigrator : BaseDataMigrator, IDataMigrator
    {
        public static readonly IDataMigrator Default = new SqlServerDataMigrator();
        private static readonly HashSet<CommonDataType> canNotOrderByTypes = new HashSet<CommonDataType>()
        {
            CommonDataType.Blob,
            CommonDataType.Text,
            CommonDataType.NText
        };
        private static string BuildColumnNames(IEnumerable<string> names) => string.Join(", ", names.Select(p => $"[{p}]"));

        private static string BuildOrderByColumnNames(TableDescriptor table)
        {
            if (table.PrimaryKey?.Columns?.Count > 0)
            {
                return BuildColumnNames(table.PrimaryKey?.Columns.ToArray());
            }

            var names = table.Columns.Where(p => !canNotOrderByTypes.Contains(p.DataType.DbType)).Select(p => p.Name);

            return BuildColumnNames(names);
        }

        public override Task<long> CountSourceTable(TableDescriptor table, AgentContext agentContext)
        {
            var sql = $"select count_big(1) from {IdentityName(table)}";
            var val = agentContext.Connection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public override Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, AgentContext agentContext)
        {
            var sql =
                $"select * from {IdentityName(table)} order by {BuildOrderByColumnNames(table)} offset {pageInfo.Offset} row fetch next {pageInfo.Limit} row only";
            return Task.FromResult(agentContext.Connection.ExecuteReaderAsTable(sql));
        }

        public override Task BeforeWriteTargetTable(TableDescriptor tableDescriptor, AgentContext agentContext)
        {
            var tableFullName = IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
            if (tableDescriptor.Columns.Any(p => p.IdentityInfo != null))
            {
                agentContext.Connection.ExecuteNonQuery($"SET IDENTITY_INSERT {tableFullName} ON");

            }

            return Task.CompletedTask;
        }

        public override Task AfterWriteTargetTable(TableDescriptor tableDescriptor, AgentContext agentContext)
        {
            if (tableDescriptor.Columns.Any(p => p.IdentityInfo != null))
            {
                var tableFullName = IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
                agentContext.Connection.ExecuteNonQuery($"SET IDENTITY_INSERT {tableFullName} OFF");

                tableDescriptor.Columns.Where(p => p.IdentityInfo?.CurrentValue != null)
                    .Each((column) =>
                    {
                        agentContext.Connection.ExecuteNonQuery($"DBCC CHECKIDENT ('{tableFullName}', RESEED, {column.IdentityInfo.CurrentValue})");
                    });
            }

            return Task.CompletedTask;
        }

        protected override Task WriteTargetTableInDefaultMode(IAsyncEnumerable<DataTable> datas, TableDescriptor table, AgentContext agentContext)
        {
            return WriteTargetTableInBlockCopyMode(datas, table, agentContext);
        }

        protected override async Task WriteTargetTableInBlockCopyMode(IAsyncEnumerable<DataTable> datas, TableDescriptor table, AgentContext agentContext)
        {
            agentContext.Connection.TryOpen();
            var options = SqlBulkCopyOptions.Default | SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls;
            await foreach (var datatable in datas)
            {
                if (datatable.Rows.Count == 0) continue;
                using var bulkCopy = new SqlBulkCopy(agentContext.Connection as SqlConnection, options, null)
                {
                    DestinationTableName = IdentityName(table),
                    BatchSize = datatable.Rows.Count,
                };
                bulkCopy.WriteToServer(datatable);
            }
        }
    }
}
