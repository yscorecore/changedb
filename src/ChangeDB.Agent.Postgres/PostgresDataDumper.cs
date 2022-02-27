using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;
using static ChangeDB.Agent.Postgres.PostgresUtils;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataDumper : BaseDataDumper
    {
        public static readonly IDataDumper Default = new PostgresDataDumper();

        public override async Task WriteTables(IAsyncEnumerable<DataTable> datas, TableDescriptor tableDescriptor, DumpContext dumpContext)
        {
            if (dumpContext.Setting.OptimizeInsertion)
            {
                await dumpContext.Writer.WriteLineAsync($"COPY {IdentityName(tableDescriptor.Schema, tableDescriptor.Name)}({BuildColumnNames(tableDescriptor.Columns)}) FROM STDIN;");

                await foreach (var dataTable in datas)
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var values = tableDescriptor.Columns.Select(p => FormatValue(row[p.Name]));
                        await dumpContext.Writer.WriteLineAsync(string.Join('\t', values));
                    }
                }
                await dumpContext.Writer.WriteLineAsync("\\.");
                await dumpContext.Writer.WriteLineAsync("");
                // copy mode

            }
            else
            {
                // insert mode
                await base.WriteTables(datas, tableDescriptor, dumpContext);
            }
        }


        protected override string IdentityName(string schema, string name)
        {
            return PostgresUtils.IdentityName(schema, name);
        }

        protected override string ReprValue(ColumnDescriptor column, object val)
        {
            return PostgresRepr.ReprConstant(val);
        }

        protected override string BuildInsertCommand(DataRow row, TableDescriptor table)
        {
            var columns = table.Columns;
            var columnValues = columns.Select(p => (Column: p, Value: row[p.Name]));
            var overIdentity = OverIdentityType(table);
            if (string.IsNullOrEmpty(overIdentity))
            {
                return $"INSERT INTO {IdentityName(table.Schema, table.Name)}({BuildColumnNames(columns)}) VALUES ({BuildColumnValues(columnValues)});";

            }
            else
            {
                return $"INSERT INTO {IdentityName(table.Schema, table.Name)}({BuildColumnNames(columns)}) {overIdentity} VALUES ({BuildColumnValues(columnValues)});";

            }
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


        private string FormatValue(object value)
        {
            return PostgresRepr.ReprCopyConstant(value);
        }


    }
}
