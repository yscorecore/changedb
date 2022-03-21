using System;
namespace ChangeDB
{
    public class DataTypeDescriptor
    {

        public CommonDataType DbType { get; set; }
        public int? Arg1 { get; set; }
        public int? Arg2 { get; set; }
        public Type ClrType { get; set; }
        public static DataTypeDescriptor Boolean() => new() { DbType = CommonDataType.Boolean, ClrType = typeof(bool) };
        public static DataTypeDescriptor TinyInt() => new() { DbType = CommonDataType.TinyInt, ClrType = typeof(byte) };
        public static DataTypeDescriptor Int() => new() { DbType = CommonDataType.Int, ClrType = typeof(int) };
        public static DataTypeDescriptor SmallInt() => new() { DbType = CommonDataType.SmallInt, ClrType = typeof(short) };
        public static DataTypeDescriptor BigInt() => new() { DbType = CommonDataType.BigInt, ClrType = typeof(long) };
        public static DataTypeDescriptor Uuid() => new() { DbType = CommonDataType.Uuid, ClrType = typeof(Guid) };
        public static DataTypeDescriptor Text() => new() { DbType = CommonDataType.Text, ClrType = typeof(string) };
        public static DataTypeDescriptor NText() => new() { DbType = CommonDataType.NText, ClrType = typeof(string) };
        public static DataTypeDescriptor Blob() => new() { DbType = CommonDataType.Blob, ClrType = typeof(byte[]) };
        public static DataTypeDescriptor Float() => new() { DbType = CommonDataType.Float, ClrType = typeof(float) };
        public static DataTypeDescriptor Double() => new() { DbType = CommonDataType.Double, ClrType = typeof(double) };
        public static DataTypeDescriptor Decimal(int size, int scale) => new() { DbType = CommonDataType.Decimal, Arg1 = size, Arg2 = scale, ClrType = typeof(decimal) };
        public static DataTypeDescriptor Char(int length) => new() { DbType = CommonDataType.Char, Arg1 = length, ClrType = typeof(string) };
        public static DataTypeDescriptor NChar(int length) => new() { DbType = CommonDataType.NChar, Arg1 = length, ClrType = typeof(string) };
        public static DataTypeDescriptor Varchar(int length) => new() { DbType = CommonDataType.Varchar, Arg1 = length, ClrType = typeof(string) };
        public static DataTypeDescriptor NVarchar(int length) => new() { DbType = CommonDataType.NVarchar, Arg1 = length, ClrType = typeof(string) };
        public static DataTypeDescriptor Binary(int length) => new() { DbType = CommonDataType.Binary, Arg1 = length, ClrType = typeof(byte[]) };
        public static DataTypeDescriptor Varbinary(int length) => new() { DbType = CommonDataType.Varbinary, Arg1 = length, ClrType = typeof(byte[]) };

        public static DataTypeDescriptor Date() => new() { DbType = CommonDataType.Date, ClrType = typeof(DateTime) };

        public static DataTypeDescriptor Time(int scale) => new() { DbType = CommonDataType.Time, Arg1 = scale, ClrType = typeof(TimeSpan) };
        public static DataTypeDescriptor DateTime(int scale) => new() { DbType = CommonDataType.DateTime, Arg1 = scale, ClrType = typeof(DateTime) };

        public static DataTypeDescriptor DateTimeOffset(int scale) => new() { DbType = CommonDataType.DateTimeOffset, Arg1 = scale, ClrType = typeof(DateTimeOffset) };

        public static DataTypeDescriptor UnKnow() => new() { DbType = CommonDataType.UnKnow,ClrType = typeof(object)};
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

        UnKnow,
    }
}
