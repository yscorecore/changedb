using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ChangeDB.Agent.Sqlite
{
    internal class SqliteDataTypeMapper
    {
        public static SqliteDataTypeMapper Default = new SqliteDataTypeMapper();

        public DataTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            _ = storeType ?? throw new ArgumentNullException(nameof(storeType));
            var match = Regex.Match(storeType.ToLowerInvariant(), @"^(?<name>[\w\s]+)(\((?<arg1>\w+)(,\s*(?<arg2>\w+))?\))?$");
            var type = match.Groups["name"].Value;
            string arg1 = match.Groups["arg1"].Value;
            string arg2 = match.Groups["arg2"].Value;
            bool isMax = arg1 == "max";
            int? length = (isMax || string.IsNullOrEmpty(arg1)) ? null : int.Parse(arg1);
            int? scale = string.IsNullOrEmpty(arg2) ? null : int.Parse(arg2);
            return type switch
            {
                "integer" => DataTypeDescriptor.Int(),
                "real" => DataTypeDescriptor.Double(),
                "text" => DataTypeDescriptor.NText(),
                "blob" => DataTypeDescriptor.Blob(),

                #region MySQL
                // number
                "bool" => DataTypeDescriptor.Boolean(),
                "tinyint" => length == 1 ? DataTypeDescriptor.Boolean() : DataTypeDescriptor.TinyInt(),
                "tinyint unsigned" => DataTypeDescriptor.TinyInt(),// TODO handle overflow
                "smallint" => DataTypeDescriptor.SmallInt(),
                "smallint unsigned" => DataTypeDescriptor.SmallInt(),// TODO handle overflow
                "mediumint" => DataTypeDescriptor.Int(),
                "mediumint unsigned" => DataTypeDescriptor.Int(),
                "int" => DataTypeDescriptor.Int(),
                "int unsigned" => DataTypeDescriptor.Int(), // TODO handle overflow
                "bigint" => DataTypeDescriptor.BigInt(),
                "bigint unsigned" => DataTypeDescriptor.BigInt(),// TODO handle overflow
                "bit" => length == null || length == 1 ? DataTypeDescriptor.Boolean() : DataTypeDescriptor.BigInt(),
                "decimal" => DataTypeDescriptor.Decimal(length ?? 10, scale ?? 0),
                "float" => DataTypeDescriptor.Float(),
                "double" => DataTypeDescriptor.Double(),

                // datetime
                "timestamp" => DataTypeDescriptor.DateTime(length ?? 0),
                "datetime" => DataTypeDescriptor.DateTime(length ?? 0),
                "date" => DataTypeDescriptor.Date(),
                "time" => DataTypeDescriptor.Time(length ?? 0),
                "year" => DataTypeDescriptor.Int(),

                // text
                "char" => DataTypeDescriptor.NChar(length ?? 1),
                "varchar" => DataTypeDescriptor.Varchar(length ?? 1),
                "tinytext" => DataTypeDescriptor.NText(),
                "mediumtext" => DataTypeDescriptor.NText(),
                "longtext" => DataTypeDescriptor.NText(),
                "json" => DataTypeDescriptor.NText(),

                //binary
                "binary" => length == 16 ? DataTypeDescriptor.Uuid() : DataTypeDescriptor.Binary(length ?? 1),
                "varbinary" => DataTypeDescriptor.Varbinary(length ?? 1),
                "tinyblob" => DataTypeDescriptor.Blob(),
                "mediumblob" => DataTypeDescriptor.Blob(),
                "longblob" => DataTypeDescriptor.Blob(),
                #endregion

                #region SQL Server
                "numeric" => DataTypeDescriptor.Decimal(length ?? 0, scale ?? 0),
                "rowversion" => DataTypeDescriptor.Binary(8),
                "uniqueidentifier" => DataTypeDescriptor.Uuid(),
                "ntext" => DataTypeDescriptor.NText(),
                "image" => DataTypeDescriptor.Blob(),
                "smallmoney" => DataTypeDescriptor.Decimal(10, 4),
                "money" => DataTypeDescriptor.Decimal(19, 4),
                "nchar" => DataTypeDescriptor.NChar(length ?? 1),
                "nvarchar" => isMax ? DataTypeDescriptor.NText() : DataTypeDescriptor.NVarchar(length ?? 1),
                "xml" => DataTypeDescriptor.NText(),
                "smalldatetime" => DataTypeDescriptor.DateTime(0),
                "datetime2" => DataTypeDescriptor.DateTime(length ?? 7),
                "datetimeoffset" => DataTypeDescriptor.DateTimeOffset(length ?? 7),
                #endregion

                #region Postgres
                "character varying" => length == null ? DataTypeDescriptor.NText() : DataTypeDescriptor.NVarchar(length.Value),
                "character" => DataTypeDescriptor.NChar(length ?? 1),
                "double precision" => DataTypeDescriptor.Double(),
                "uuid" => DataTypeDescriptor.Uuid(),
                "bytea" => DataTypeDescriptor.Blob(),
                "timestamp without time zone" => DataTypeDescriptor.DateTime(length ?? 6),
                "timestamp with time zone" => DataTypeDescriptor.DateTimeOffset(length ?? 6),
                "time without time zone" => DataTypeDescriptor.Time(length ?? 6),
                "boolean" => DataTypeDescriptor.Boolean(),
                #endregion

                _ => DataTypeDescriptor.UnKnow()
            };
        }

        public string ToDatabaseStoreType(DataTypeDescriptor commonDataType)
        {
            return commonDataType.DbType switch
            {
                //CommonDataType.Boolean => "BLOB",
                CommonDataType.TinyInt or CommonDataType.SmallInt or CommonDataType.Int or CommonDataType.BigInt => "INTEGER",
                //CommonDataType.Decimal or CommonDataType.Float or CommonDataType.Double => "REAL",
                //CommonDataType.Binary or CommonDataType.Varbinary or CommonDataType.Blob or CommonDataType.Uuid => "BLOB",
                //CommonDataType.Char or CommonDataType.NChar or CommonDataType.Varchar or CommonDataType.NVarchar or CommonDataType.Text or CommonDataType.NText or CommonDataType.Time or CommonDataType.Date or CommonDataType.DateTime or CommonDataType.DateTimeOffset => "TEXT",
                _ => GetDefaultCase(commonDataType)
            };

            string GetDefaultCase(DataTypeDescriptor desc)
            {
                var builder = new StringBuilder();
                builder.Append(desc.DbType.ToString().ToLowerInvariant());
                if (desc.Arg1 is not null)
                {
                    builder.Append('(').Append(desc.Arg1);
                }
                if (desc.Arg2 is not null)
                {
                    builder.Append(',').Append(desc.Arg2);
                }
                if (desc.Arg1 is not null)
                {
                    builder.Append(')');
                }
                return builder.ToString();
            }
        }

        private static (string Type, int? Arg1, int? Arg2) ParseStoreType(string storeType)
        {
            var index1 = storeType.IndexOf('(');
            var index2 = storeType.IndexOf(')');
            if (index1 > 0 && index2 > 0)
            {
                var type = storeType[..index1] + storeType.Substring(index2 + 1);
                var index3 = storeType.IndexOf(',', index1);
                if (index3 > 0)
                {
                    return (type, int.Parse(storeType.Substring(index1 + 1, index3 - index1 - 1).Trim()),
                        int.Parse(storeType.Substring(index3 + 1, index2 - index3 - 1).Trim()));
                }
                else
                {
                    return (type, int.Parse(storeType.Substring(index1 + 1, index2 - index1 - 1).Trim()), null);
                }
            }

            return (storeType.ToLower(), null, null);
        }
    }
}
