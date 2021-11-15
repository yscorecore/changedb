using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public class DatabaseTypeDescriptor
    {


        public CommonDatabaseType DbType { get; set; }
        public int? Size { get; set; }
        public int? Scale { get; set; }
      
        public static DatabaseTypeDescriptor Create(CommonDatabaseType commonDatabaseType, int? length = null, int? accuracy = null)
        {
            return new DatabaseTypeDescriptor
            {
                DbType = commonDatabaseType,
                Size = length,
                Scale = accuracy,
            };
        }
    }

    public enum CommonDatabaseType
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

        Double,
        Float,
        Decimal,

        Date,
        Time,
        DateTime,
        DateTimeOffset,
    }
}
