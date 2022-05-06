using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB
{
    public abstract class BaseDataMigrator : IDataMigrator
    {
        public abstract Task AfterWriteTable(TableDescriptor tableDescriptor, AgentContext agentContext);

        public abstract Task BeforeWriteTable(TableDescriptor tableDescriptor, AgentContext agentContext);

        public virtual Task<long> CountSourceTable(TableDescriptor table, AgentContext agentContext)
        {
            var sql = $"select count(1) from {agentContext.Agent.AgentSetting.IdentityName(table.Schema, table.Name)}";
            var totalCount = agentContext.Connection.ExecuteScalar<long>(sql);
            return Task.FromResult(totalCount);
        }
        public abstract Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, AgentContext agentContext);
        public virtual Task WriteTargetTable(IAsyncEnumerable<DataTable> data, TableDescriptor table, AgentContext agentContext, InsertionKind insertionKind = InsertionKind.Default)
        {
            return insertionKind switch
            {
                InsertionKind.BlockCopy => WriteTargetTableInBlockCopyMode(data, table, agentContext),
                InsertionKind.BatchRow => WriteTargetTableInBatchLineMode(data, table, agentContext),
                InsertionKind.SingleRow => WriteTargetTableInSingleLineMode(data, table, agentContext),
                _ => WriteTargetTableInDefaultMode(data, table, agentContext),
            };
        }
        protected abstract Task WriteTargetTableInDefaultMode(IAsyncEnumerable<DataTable> data, TableDescriptor table, AgentContext agentContext);
        protected virtual async Task WriteTargetTableInSingleLineMode(IAsyncEnumerable<DataTable> data, TableDescriptor table, AgentContext agentContext)
        {
            // insert into abc(id,val)
            var sqlSegment = BuildInsertRowSqlSegment(table, agentContext);
            var sql = $"{sqlSegment} VALUES ({string.Join(", ", table.Name.Select(p => $"@{p}"))});";
            await foreach (var dataTable in data)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    agentContext.Connection.ExecuteNonQuery(sql, GetRowData(row));
                }
                // TOTO REPORT PROGRESS
            }
            IDictionary<string, object> GetRowData(DataRow row)
            {
                var dic = new Dictionary<string, object>();
                table.Columns.Each(column => { dic[$"@{column.Name}"] = row[column.Name]; });
                return dic;
            }
        }

        protected virtual string BuildInsertRowSqlSegment(TableDescriptor table, AgentContext agentContext)
        {
            return $"INSERT INTO {IdentityName()} ({BuildColumnNames(table, agentContext)})";
            string IdentityName() => agentContext.Agent.AgentSetting.IdentityName(table.Schema, table.Name);
        }
        protected virtual async Task WriteTargetTableInBatchLineMode(IAsyncEnumerable<DataTable> data, TableDescriptor table, AgentContext agentContext)
        {
            const int maxCount = 1000;
            var cachedRows = new List<DataRow>();
            await foreach (var row in data.ToItems(p => p.Rows.OfType<DataRow>()))
            {
                if (cachedRows.Count < maxCount)
                {
                    cachedRows.Add(row);
                }
                else
                {
                    BatchInsert(cachedRows);
                    cachedRows.Clear();
                }
            }
            if (cachedRows.Count > 0)
            {
                BatchInsert(cachedRows);
                cachedRows.Clear();
            }
            void BatchInsert(List<DataRow> rows)
            {
                var newLine = Environment.NewLine;
                var lines = new List<string>();
                var sqlFormat = $"{BuildInsertRowSqlSegment(table, agentContext)} VALUES{newLine}{{0}};";
                var dic = new Dictionary<string, object>();
                for (int i = 0; i < rows.Count; i++)
                {
                    var names = table.Columns.Select((c, j) => (c.Name, $"@p{i}_{j}")).ToList();
                    names.ForEach(p => dic.Add(p.Item2, rows[i][p.Name]));
                    lines.Add($"({string.Join(", ", names.Select(p => p.Item2))})");
                }
                var sql = string.Format(sqlFormat, string.Join($",{newLine}", lines));
                agentContext.Connection.ExecuteNonQuery(sql, dic);
                // TOTO REPORT PROGRESS
            }
        }
        protected abstract Task WriteTargetTableInBlockCopyMode(IAsyncEnumerable<DataTable> datas, TableDescriptor table, AgentContext agentContext);
        protected static string BuildColumnNames(TableDescriptor table, AgentContext agentContext) => BuildColumnNames(table.Columns.Select(p => p.Name), agentContext);

        protected static string BuildColumnNames(IEnumerable<string> names, AgentContext agentContext) => string.Join(", ", names.Select(p => $"{agentContext.Agent.AgentSetting.IdentityName(null, p)}"));
    }
}
