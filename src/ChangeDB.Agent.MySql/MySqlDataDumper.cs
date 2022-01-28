using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;

namespace ChangeDB.Agent.MySql
{
    public class MySqlDataDumper : BaseDataDumper
    {
        public static readonly IDataDumper Default = new MySqlDataDumper();
        protected override string IdentityName(string schema, string name)
        {
            return MySqlUtils.IdentityName(schema, name);
        }

        protected override string ReprValue(ColumnDescriptor column, object val)
        {
            return MySqlRepr.ReprConstant(val);
        }
    }
}
