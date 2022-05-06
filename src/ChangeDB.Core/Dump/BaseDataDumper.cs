using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Dump
{
    public abstract class BaseDataDumper : IDataDumper
    {
        protected virtual string BuildInsertCommand(DataRow row, TableDescriptor table)
        {
            var columns = table.Columns;
            var columnValues = columns.Select(p => (Column: p, Value: row[p.Name]));
            return $"INSERT INTO {IdentityName(table.Schema, table.Name)}({BuildColumnNames(columns)}) VALUES ({BuildColumnValues(columnValues)});";
        }
        protected abstract string ReprValue(ColumnDescriptor column, object val);

        protected abstract string IdentityName(string schema, string name);
        protected virtual string BuildColumnNames(IEnumerable<ColumnDescriptor> columns)
        {
            return string.Join(", ", columns.Select(p => IdentityName(null, p.Name)));
        }
        protected virtual string BuildColumnValues(IEnumerable<(ColumnDescriptor Column, object Value)> columnValues)
        {
            return string.Join(", ", columnValues.Select(p => ReprValue(p.Column, p.Value)));
        }
        public virtual async Task WriteTables(IAsyncEnumerable<DataTable> datas, TableDescriptor tableDescriptor, DumpSetting dumpSetting)
        {

            await foreach (DataTable dataTable in datas)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    var line = BuildInsertCommand(row, tableDescriptor);
                    await dumpSetting.Writer.WriteLineAsync(line);
                    await dumpSetting.Writer.WriteLineAsync();
                }
            }
        }
    }
}
