using ChangeDB.Dump;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Sqlite
{
    public class SqliteDataDumper : BaseDataDumper
    {
        public static readonly IDataDumper Default = new SqliteDataDumper();
        protected override string IdentityName(string schema, string name)
        {
            return SqliteUtils.IdentityName(name);
        }

        protected override string ReprValue(ColumnDescriptor column, object val)
        {
            var dataType = SqliteDataTypeMapper.Default.ToDatabaseStoreType(column.DataType);

            return SqliteRepr.ReprConstant(val, dataType);
        }
    }
}
