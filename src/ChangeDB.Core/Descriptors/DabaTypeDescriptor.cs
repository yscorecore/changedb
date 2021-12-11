using System;

namespace ChangeDB
{
    public class DataTypeDescriptor
    {


        public CommonDataType DbType { get; set; }
        public int? Arg1 { get; set; }
        public int? Arg2 { get; set; }

        [Obsolete()]
        public static DataTypeDescriptor Create(CommonDataType commonDataType, int? size = null, int? scale = null)
        {
            return new DataTypeDescriptor
            {
                DbType = commonDataType,
                Arg1 = size,
                Arg2 = scale,
            };
        }
        public static DataTypeDescriptor Boolean() => new DataTypeDescriptor { DbType = CommonDataType.Boolean };
        public static DataTypeDescriptor TinyInt() => new DataTypeDescriptor { DbType = CommonDataType.TinyInt };
        public static DataTypeDescriptor Int() => new DataTypeDescriptor { DbType = CommonDataType.Int };
        public static DataTypeDescriptor SmallInt() => new DataTypeDescriptor { DbType = CommonDataType.SmallInt };
        public static DataTypeDescriptor BigInt() => new DataTypeDescriptor { DbType = CommonDataType.BigInt };
        public static DataTypeDescriptor Uuid() => new DataTypeDescriptor { DbType = CommonDataType.Uuid };
        public static DataTypeDescriptor Text() => new DataTypeDescriptor { DbType = CommonDataType.Text };
        public static DataTypeDescriptor NText() => new DataTypeDescriptor { DbType = CommonDataType.NText };
        public static DataTypeDescriptor Blob() => new DataTypeDescriptor { DbType = CommonDataType.Blob };
        public static DataTypeDescriptor Float() => new DataTypeDescriptor { DbType = CommonDataType.Float };
        public static DataTypeDescriptor Double() => new DataTypeDescriptor { DbType = CommonDataType.Double };
        public static DataTypeDescriptor Decimal(int size, int scale) => new DataTypeDescriptor { DbType = CommonDataType.Decimal, Arg1 = size, Arg2 = scale };
        public static DataTypeDescriptor Char(int length) => new DataTypeDescriptor { DbType = CommonDataType.Char, Arg1 = length };
        public static DataTypeDescriptor NChar(int length) => new DataTypeDescriptor { DbType = CommonDataType.NChar, Arg1 = length };
        public static DataTypeDescriptor Varchar(int length) => new DataTypeDescriptor { DbType = CommonDataType.Varchar, Arg1 = length };
        public static DataTypeDescriptor NVarchar(int length) => new DataTypeDescriptor { DbType = CommonDataType.NVarchar, Arg1 = length };
        public static DataTypeDescriptor Binary(int length) => new DataTypeDescriptor { DbType = CommonDataType.Binary, Arg1 = length };
        public static DataTypeDescriptor Varbinary(int length) => new DataTypeDescriptor { DbType = CommonDataType.Varbinary, Arg1 = length };

        public static DataTypeDescriptor Date() => new DataTypeDescriptor { DbType = CommonDataType.Date };

        public static DataTypeDescriptor Time(int scale) => new DataTypeDescriptor { DbType = CommonDataType.Time, Arg1 = scale };
        public static DataTypeDescriptor DateTime(int scale) => new DataTypeDescriptor { DbType = CommonDataType.DateTime, Arg1 = scale };

        public static DataTypeDescriptor DateTimeOffset(int scale) => new DataTypeDescriptor { DbType = CommonDataType.DateTimeOffset, Arg1 = scale };
    }

    public enum CommonDataType
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
