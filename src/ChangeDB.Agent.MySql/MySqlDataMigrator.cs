using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.MySql.MySqlUtils;

namespace ChangeDB.Agent.MySql
{
    public class MySqlDataMigrator : BaseDataMigrator, IDataMigrator
    {
        public static readonly IDataMigrator Default = new MySqlDataMigrator();

        public override Task<long> CountSourceTable(TableDescriptor table, AgentContext agentContext)
        {
            var sql = $"select count(1) from {IdentityName(table)}";
            var val = agentContext.Connection.ExecuteScalar<long>(sql);
            return Task.FromResult(val);
        }

        public override Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, AgentContext agentContext)
        {
            var sql =
                $"select * from {IdentityName(table)} limit {pageInfo.Limit} offset {pageInfo.Offset}";
            return Task.FromResult(agentContext.Connection.ExecuteReaderAsTable(sql));
        }

        public override Task BeforeWriteTargetTable(TableDescriptor tableDescriptor, AgentContext agentContext)
        {
            return Task.CompletedTask;
        }

        public override Task AfterWriteTargetTable(TableDescriptor tableDescriptor, AgentContext agentContext)
        {
            var identityColumn = tableDescriptor.Columns.FirstOrDefault(p => p.IsIdentity && p.IdentityInfo?.CurrentValue != null);
            if (identityColumn != null)
            {
                var tableFullName = IdentityName(tableDescriptor.Schema, tableDescriptor.Name);
                agentContext.Connection.ExecuteNonQuery($"ALTER TABLE {tableFullName} AUTO_INCREMENT = {identityColumn.IdentityInfo.CurrentValue + identityColumn.IdentityInfo.IncrementBy }");
            }
            return Task.CompletedTask;
        }

        protected override Task WriteTargetTableInDefaultMode(IAsyncEnumerable<DataTable> datas, TableDescriptor table, AgentContext agentContext)
        {
            return WriteTargetTableInBatchLineMode(datas, table, agentContext);
        }

        protected override Task WriteTargetTableInBlockCopyMode(IAsyncEnumerable<DataTable> datas, TableDescriptor table, AgentContext agentContext)
        {
            //TODO report warnning
            return WriteTargetTableInBatchLineMode(datas, table, agentContext);
        }
    }
}
