using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;
using static ChangeDB.Agent.Postgres.PostgresUtils;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataDumper : BaseDataDumper
    {
        public static readonly IDataDumper Default = new PostgresDataDumper();

        public override async Task WriteTable(DataTable data, TableDescriptor table, DumpContext dumpContext)
        {
            var setting = dumpContext.Setting;
            if (setting.OptimizeInsertion)
            {
                //Copy mode
                await CopyTable(data, table, dumpContext);
            }
            else
            {
                //Insert mode
                await base.WriteTable(data, table, dumpContext);
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


        private Task CopyTable(DataTable data, TableDescriptor table, DumpContext dumpContext)
        {
            return Task.CompletedTask;
        }

    }
}
