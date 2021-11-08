using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public class DBTypeDescriptor
    {
        public DBType DbType { get; set; }
        public int? Length { get; set; }
        public int? Accuracy { get; set; }
        public override string ToString()
        {
            if (Length is null)
            {
                return Type2String(DbType);
            }
            else
            {
                if (Accuracy is null)
                {
                    return $"{Type2String(DbType)}({Length})";
                }
                else
                {
                    return $"{Type2String(DbType)}({Length},{Accuracy})";
                }
            }
        }



        private string Type2String(DBType dbType) => $"{dbType.ToString().ToUpperInvariant().Replace("__", " ")}";
    }

    public enum DBType
    {
        // int32
        Int,
        // int16
        SmallInt,
        // int64
        BigInt,
        // int8
        TinyInt,



        Char,
        Varchar,
        Text,
        NChar,
        NVarchar,
        NText,
        Boolean,

        Binary,
        Varbinary,
        Blob,

        Uuid,
        RowVersion,

        Double,
        Float,
        Decimal,

        Date,
        Time,
        DateTime,
        DateTimeOffset,
    }
}
