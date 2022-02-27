using ChangeDB.Dump;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDataDumper : BaseDataDumper
    {
        public static readonly IDataDumper Default = new SqlServerDataDumper();
        protected override string IdentityName(string schema, string name)
        {
            return SqlServerUtils.IdentityName(schema, name);
        }

        protected override string ReprValue(ColumnDescriptor column, object val)
        {
            return SqlServerRepr.ReprConstant(val);
        }
    }
}
