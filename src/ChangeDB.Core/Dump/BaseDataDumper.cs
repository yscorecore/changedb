using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Dump
{
    public abstract class BaseDataDumper : IDataDumper
    {
        public virtual Task WriteTable(DataTable data, TableDescriptor table, DumpContext dumpContext)
        {
            foreach (DataRow row in data.Rows)
            {
                var line = BuildInsertCommand(row, table);
                dumpContext.Writer.WriteLine(line);
                dumpContext.Writer.WriteLine();
            }
            return Task.CompletedTask;
        }

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

    }
}
