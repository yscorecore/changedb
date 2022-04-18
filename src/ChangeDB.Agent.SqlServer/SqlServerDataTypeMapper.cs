using System;
using System.Text.RegularExpressions;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    internal class SqlServerDataTypeMapper
    {
        public static SqlServerDataTypeMapper Default = new SqlServerDataTypeMapper();


        public DataTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            _ = storeType ?? throw new ArgumentNullException(nameof(storeType));
            var match = Regex.Match(storeType.ToLowerInvariant(), @"^(?<name>\w+)(\((?<arg1>\w+)(,\s*(?<arg2>\w+))?\))?$");
            var type = match.Groups["name"].Value;
            string arg1 = match.Groups["arg1"].Value;
            string arg2 = match.Groups["arg2"].Value;
            bool isMax = arg1 == "max";
            int? length = (isMax || string.IsNullOrEmpty(arg1)) ? null : int.Parse(arg1);
            int? scale = string.IsNullOrEmpty(arg2) ? null : int.Parse(arg2);
            return type switch
            {
                "bit" => DataTypeDescriptor.Boolean(),
                "tinyint" => DataTypeDescriptor.TinyInt(),
                "smallint" => DataTypeDescriptor.SmallInt(),
                "int" => DataTypeDescriptor.Int(),
                "bigint" => DataTypeDescriptor.BigInt(),
                "decimal" => DataTypeDescriptor.Decimal(length ?? 0, scale ?? 0),
                "numeric" => DataTypeDescriptor.Decimal(length ?? 0, scale ?? 0),
                "timestamp" => DataTypeDescriptor.Binary(8),
                "rowversion" => DataTypeDescriptor.Binary(8),
                "uniqueidentifier" => DataTypeDescriptor.Uuid(),
                "real" => DataTypeDescriptor.Float(),
                "text" => DataTypeDescriptor.Text(),
                "ntext" => DataTypeDescriptor.NText(),
                "image" => DataTypeDescriptor.Blob(),
                "float" => DataTypeDescriptor.Double(),
                "smallmoney" => DataTypeDescriptor.Decimal(10, 4),
                "money" => DataTypeDescriptor.Decimal(19, 4),
                "binary" => DataTypeDescriptor.Binary(length ?? 1),
                "varbinary" => isMax ? DataTypeDescriptor.Blob() : DataTypeDescriptor.Varbinary(length ?? 1),
                "char" => DataTypeDescriptor.Char(length ?? 1),
                "nchar" => DataTypeDescriptor.NChar(length ?? 1),
                "varchar" => isMax ? DataTypeDescriptor.Text() : DataTypeDescriptor.Varchar(length ?? 1),
                "nvarchar" => isMax ? DataTypeDescriptor.NText() : DataTypeDescriptor.NVarchar(length ?? 1),
                "xml" => DataTypeDescriptor.NText(),
                "date" => DataTypeDescriptor.Date(),
                "time" => DataTypeDescriptor.Time(length ?? 7),
                "datetime" => DataTypeDescriptor.DateTime(3),
                "smalldatetime" => DataTypeDescriptor.DateTime(0),
                "datetime2" => DataTypeDescriptor.DateTime(length ?? 7),
                "datetimeoffset" => DataTypeDescriptor.DateTimeOffset(length ?? 7),
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
                CommonDataType.Char => $"char({commonDataType.Arg1})",
                CommonDataType.NChar => $"nchar({commonDataType.Arg1})",
                CommonDataType.Varchar => $"varchar({commonDataType.Arg1})",
                CommonDataType.NVarchar => $"nvarchar({commonDataType.Arg1})",
                CommonDataType.Text => "text",
                CommonDataType.NText => "ntext",
                CommonDataType.Time => $"time({commonDataType.Arg1})",
                CommonDataType.Date => "date",
                CommonDataType.DateTime => $"datetime2({commonDataType.Arg1})",
                CommonDataType.DateTimeOffset => $"datetimeoffset({commonDataType.Arg1})",
                _ => throw new NotSupportedException($"can not convert from common database type {commonDataType}")
            };
        }
    }
}
