using System;
using System.Text.RegularExpressions;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlCeDataTypeMapper : IDataTypeMapper
    {
        public static IDataTypeMapper Default = new SqlCeDataTypeMapper();

        //https://docs.microsoft.com/en-us/previous-versions/sql/compact/sql-server-compact-4.0/ms172424(v=sql.110)?redirectedfrom=MSDN
        public DataTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            _ = storeType ?? throw new ArgumentNullException(nameof(storeType));
            var match = Regex.Match(storeType.ToLowerInvariant(), @"^(?<name>\w+)(\((?<arg1>\w+)(,\s*(?<arg2>\w+))?\))?$");
            var type = match.Groups["name"].Value;
            string arg1 = match.Groups["arg1"].Value;
            string arg2 = match.Groups["arg2"].Value;
            int length = string.IsNullOrEmpty(arg1) ? default : int.Parse(arg1);
            int scale = string.IsNullOrEmpty(arg2) ? default : int.Parse(arg2);
            return type switch
            {
                "bit" => DataTypeDescriptor.Boolean(),
                "tinyint" => DataTypeDescriptor.TinyInt(),
                "smallint" => DataTypeDescriptor.SmallInt(),
                "int" => DataTypeDescriptor.Int(),
                "bigint" => DataTypeDescriptor.BigInt(),
                "decimal" => DataTypeDescriptor.Decimal(length, scale),
                "numeric" => DataTypeDescriptor.Decimal(length, scale),
                "timestamp" => DataTypeDescriptor.Binary(8),
                "rowversion" => DataTypeDescriptor.Binary(8),
                "uniqueidentifier" => DataTypeDescriptor.Uuid(),
                "real" => DataTypeDescriptor.Float(),
                "ntext" => DataTypeDescriptor.NText(),
                "image" => DataTypeDescriptor.Blob(),
                "float" => DataTypeDescriptor.Double(),
                "money" => DataTypeDescriptor.Decimal(19, 4),
                "binary" => DataTypeDescriptor.Binary(length),
                "varbinary" => DataTypeDescriptor.Varbinary(length),
                "nchar" => DataTypeDescriptor.NChar(length),
                "nvarchar" => DataTypeDescriptor.NVarchar(length),
                "datetime" => DataTypeDescriptor.DateTime(3),
                _ => throw new System.NotSupportedException($"not support dbtype {storeType}.")
            };
        }

        public string ToDatabaseStoreType(DataTypeDescriptor commonDataType)
        {
            return commonDataType.DbType switch
            {
                CommonDataType.Boolean => "bit",
                CommonDataType.TinyInt => "tinyint",
                CommonDataType.SmallInt => "smallint",
                CommonDataType.Int => "int",
                CommonDataType.BigInt => "bigint",
                CommonDataType.Decimal => $"decimal({commonDataType.Arg1},{commonDataType.Arg2})",
                CommonDataType.Float => "real",
                CommonDataType.Double => "float",
                CommonDataType.Binary => $"binary({commonDataType.Arg1})",
                CommonDataType.Varbinary => $"varbinary({commonDataType.Arg1})",
                CommonDataType.Blob => "image",
                CommonDataType.Uuid => "uniqueidentifier",
                CommonDataType.Char => $"nchar({commonDataType.Arg1})",
                CommonDataType.NChar => $"nchar({commonDataType.Arg1})",
                CommonDataType.Varchar => $"nvarchar({commonDataType.Arg1})",
                CommonDataType.NVarchar => $"nvarchar({commonDataType.Arg1})",
                CommonDataType.Text => "ntext",
                CommonDataType.NText => "ntext",
                CommonDataType.Time => $"datetime",
                CommonDataType.Date => "datetime",
                CommonDataType.DateTime => $"datetime",
                CommonDataType.DateTimeOffset => $"datetime",
                _ => throw new NotSupportedException($"can not convert from common database type {commonDataType}")
            };
        }
    }
}
