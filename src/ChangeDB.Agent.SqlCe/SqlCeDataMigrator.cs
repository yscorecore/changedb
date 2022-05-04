using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.SqlCe.SqlCeUtils;
namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDataMigrator : BaseDataMigrator
    {
        public static readonly IDataMigrator Default = new SqlCeDataMigrator();

        public override Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, AgentContext agentContext)
        {
            string BuildPrimaryKeyColumnNames(TableDescriptor table)
            {
                if (table.PrimaryKey?.Columns?.Count > 0)
                {
                    return BuildColumnNames(table.PrimaryKey?.Columns, agentContext);
                }
                return BuildColumnNames(table, agentContext);
            }

            var sql =
                $"select * from {IdentityName(table)} order by {BuildPrimaryKeyColumnNames(table)} offset {pageInfo.Offset} row fetch next {pageInfo.Limit} row only";
            return Task.FromResult(agentContext.Connection.ExecuteReaderAsTable(sql));
        }


        public override Task BeforeWriteTargetTable(TableDescriptor tableDescriptor, AgentContext agentContext)
        {
            var tableFullName = SqlCeUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
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
                var tableFullName = SqlCeUtils.IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
                agentContext.Connection.ExecuteNonQuery($"SET IDENTITY_INSERT {tableFullName} OFF");

                tableDescriptor.Columns.Where(p => p.IdentityInfo?.CurrentValue != null)
                    .Each((column) =>
                    {
                        var startValue = column.IdentityInfo.CurrentValue + column.IdentityInfo.IncrementBy;
                        var incrementBy = column.IdentityInfo.IncrementBy;
                        agentContext.Connection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {SqlCeUtils.IdentityName(column.Name)} IDENTITY({startValue},{incrementBy})");
                    });
            }

            return Task.CompletedTask;
        }



        protected override Task WriteTargetTableInDefaultMode(IAsyncEnumerable<DataTable> data, TableDescriptor table, AgentContext agentContext)
        {
            return WriteTargetTableInBatchLineMode(data, table, agentContext);
        }

        protected override Task WriteTargetTableInBlockCopyMode(IAsyncEnumerable<DataTable> datas, TableDescriptor table, AgentContext agentContext)
        {
            // TODO warnning
            return WriteTargetTableInBatchLineMode(datas, table, agentContext);
        }
    }
}
