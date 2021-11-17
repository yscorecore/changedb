using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public class DatabaseTypeDescriptor
    {


        public CommonDatabaseType DbType { get; set; }
        public int? Arg1 { get; set; }
        public int? Arg2 { get; set; }
      
        [Obsolete()]
        public static DatabaseTypeDescriptor Create(CommonDatabaseType commonDatabaseType, int? size = null, int? scale = null)
        {
            return new DatabaseTypeDescriptor
            {
                DbType = commonDatabaseType,
                Arg1 = size,
                Arg2 = scale,
            };
        }
        public static DatabaseTypeDescriptor Boolean() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Boolean};
        public static DatabaseTypeDescriptor TinyInt() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.TinyInt};
        public static DatabaseTypeDescriptor Int() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Int};
        public static DatabaseTypeDescriptor SmallInt() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.SmallInt};
        public static DatabaseTypeDescriptor BigInt() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.BigInt};
        public static DatabaseTypeDescriptor Uuid() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Uuid};
        public static DatabaseTypeDescriptor Text() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Text};
        public static DatabaseTypeDescriptor NText() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.NText};
        public static DatabaseTypeDescriptor Blob() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Blob};
        public static DatabaseTypeDescriptor Float() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Float};
        public static DatabaseTypeDescriptor Double() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Double};
        public static DatabaseTypeDescriptor Decimal(int size, int scale) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Decimal, Arg1 = size, Arg2 = scale};
        public static DatabaseTypeDescriptor Char(int length) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Char, Arg1 = length};
        public static DatabaseTypeDescriptor NChar(int length) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.NChar, Arg1 = length};
        public static DatabaseTypeDescriptor Varchar(int length) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Varchar, Arg1 = length};
        public static DatabaseTypeDescriptor NVarchar(int length) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.NVarchar, Arg1 = length};
        public static DatabaseTypeDescriptor Binary(int length) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Binary, Arg1 = length};
        public static DatabaseTypeDescriptor Varbinary(int length) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Varbinary, Arg1 = length};

        public static DatabaseTypeDescriptor Date() => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Date};

        public static DatabaseTypeDescriptor Time(int scale) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.Time,Arg1 = scale};
        public static DatabaseTypeDescriptor DateTime(int scale) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.DateTime,Arg1 = scale};
        
        public static DatabaseTypeDescriptor DateTimeOffset(int scale) => new DatabaseTypeDescriptor {DbType = CommonDatabaseType.DateTimeOffset,Arg1 = scale};
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
