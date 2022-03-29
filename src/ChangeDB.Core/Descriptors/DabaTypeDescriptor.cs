using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ChangeDB
{
    public class DataTypeDescriptor
    {

        public CommonDataType DbType { get; set; }
        public int? Arg1 { get; set; }
        public int? Arg2 { get; set; }
        public static DataTypeDescriptor Boolean() => new() { DbType = CommonDataType.Boolean };
        public static DataTypeDescriptor TinyInt() => new() { DbType = CommonDataType.TinyInt };
        public static DataTypeDescriptor Int() => new() { DbType = CommonDataType.Int };
        public static DataTypeDescriptor SmallInt() => new() { DbType = CommonDataType.SmallInt,  };
        public static DataTypeDescriptor BigInt() => new() { DbType = CommonDataType.BigInt };
        public static DataTypeDescriptor Uuid() => new() { DbType = CommonDataType.Uuid };
        public static DataTypeDescriptor Text() => new() { DbType = CommonDataType.Text};
        public static DataTypeDescriptor NText() => new() { DbType = CommonDataType.NText};
        public static DataTypeDescriptor Blob() => new() { DbType = CommonDataType.Blob};
        public static DataTypeDescriptor Float() => new() { DbType = CommonDataType.Float };
        public static DataTypeDescriptor Double() => new() { DbType = CommonDataType.Double };
        public static DataTypeDescriptor Decimal(int size, int scale) => new() { DbType = CommonDataType.Decimal, Arg1 = size, Arg2 = scale };
        public static DataTypeDescriptor Char(int length) => new() { DbType = CommonDataType.Char, Arg1 = length};
        public static DataTypeDescriptor NChar(int length) => new() { DbType = CommonDataType.NChar, Arg1 = length };
        public static DataTypeDescriptor Varchar(int length) => new() { DbType = CommonDataType.Varchar, Arg1 = length };
        public static DataTypeDescriptor NVarchar(int length) => new() { DbType = CommonDataType.NVarchar, Arg1 = length };
        public static DataTypeDescriptor Binary(int length) => new() { DbType = CommonDataType.Binary, Arg1 = length };
        public static DataTypeDescriptor Varbinary(int length) => new() { DbType = CommonDataType.Varbinary, Arg1 = length };

        public static DataTypeDescriptor Date() => new() { DbType = CommonDataType.Date };

        public static DataTypeDescriptor Time(int scale) => new() { DbType = CommonDataType.Time, Arg1 = scale};
        public static DataTypeDescriptor DateTime(int scale) => new() { DbType = CommonDataType.DateTime, Arg1 = scale };

        public static DataTypeDescriptor DateTimeOffset(int scale) => new() { DbType = CommonDataType.DateTimeOffset, Arg1 = scale };

        public static DataTypeDescriptor UnKnow() => new() { DbType = CommonDataType.UnKnow};
    }

    public static class DataTypeDescriptorExtensions
    {
        public static Type GetClrType(this DataTypeDescriptor dataTypeDescriptor)
        {
            return DataTypeMapperAttribute.TypeMappers.TryGetValue(dataTypeDescriptor.DbType, out var type)
                ? type
                : default;
        }
    }

    public enum CommonDataType
    {
        [DataTypeMapper(typeof(int))]
        Int,
        [DataTypeMapper(typeof(short))]
        SmallInt,
        [DataTypeMapper(typeof(long))]
        BigInt,
        [DataTypeMapper(typeof(byte))]
        TinyInt,

        [DataTypeMapper(typeof(string))]
        Char,
        [DataTypeMapper(typeof(string))]
        Varchar,
        [DataTypeMapper(typeof(string))]
        Text,
        [DataTypeMapper(typeof(string))]
        NChar,
        [DataTypeMapper(typeof(string))]
        NVarchar,
        [DataTypeMapper(typeof(string))]
        NText,
        [DataTypeMapper(typeof(bool))]
        Boolean,
        [DataTypeMapper(typeof(byte[]))]
        Binary,
        [DataTypeMapper(typeof(byte[]))]
        Varbinary,
        [DataTypeMapper(typeof(byte[]))]
        Blob,
        [DataTypeMapper(typeof(Guid))]
        Uuid,
        [DataTypeMapper(typeof(double))]
        Double,
        [DataTypeMapper(typeof(float))]
        Float,
        [DataTypeMapper(typeof(decimal))]
        Decimal,
        [DataTypeMapper(typeof(DateTime))]
        Date,
        [DataTypeMapper(typeof(DateTime))]
        Time,
        [DataTypeMapper(typeof(TimeSpan))]
        DateTime,
        [DataTypeMapper(typeof(DateTimeOffset))]
        DateTimeOffset,
        [DataTypeMapper(typeof(object))]
        UnKnow,
    }

    [AttributeUsage(AttributeTargets.Field,AllowMultiple = false,Inherited = false)]
    internal class DataTypeMapperAttribute : Attribute
    {
        internal static Dictionary<CommonDataType, Type> TypeMappers = typeof(CommonDataType).GetFields()
            .Where(f=>Attribute.IsDefined(f,typeof(DataTypeMapperAttribute)))
            .ToDictionary(f => (CommonDataType)f.GetValue(null),
                f => f.GetCustomAttribute<DataTypeMapperAttribute>()?.ClrType);
        public DataTypeMapperAttribute(Type clrType)
        {
            this.ClrType = clrType;
        }

        public Type ClrType { get; private set; }
        
        
    }
}
