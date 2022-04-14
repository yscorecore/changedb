using ChangeDB.Dump;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDataDumper : BaseDataDumper
    {
        public static readonly IDataDumper Default = new SqlCeDataDumper();
        protected override string IdentityName(string schema, string name)
        {
            return SqlCeUtils.IdentityName(schema, name);
        }

        protected override string ReprValue(ColumnDescriptor column, object val)
        {
            return SqlCeRepr.ReprConstant(val);
        }
    }
}
