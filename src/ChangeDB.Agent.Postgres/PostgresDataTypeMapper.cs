using System;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataTypeMapper
    {

        public static readonly PostgresDataTypeMapper Default = new PostgresDataTypeMapper();
        public DataTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            _ = storeType ?? throw new ArgumentNullException(nameof(storeType));
            var (type, arg1, arg2) = ParseStoreType(storeType);
            return type.ToUpperInvariant() switch
            {
                "CHARACTER VARYING" => arg1 == null ? DataTypeDescriptor.NText() : DataTypeDescriptor.NVarchar(arg1.Value),
                "CHARACTER" => DataTypeDescriptor.NChar(arg1.Value),
                "TEXT" => DataTypeDescriptor.NText(),
                "INTEGER" => DataTypeDescriptor.Int(),
                "BIGINT" => DataTypeDescriptor.BigInt(),
                "SMALLINT" => DataTypeDescriptor.SmallInt(),
                "TINYINT" => DataTypeDescriptor.SmallInt(),
                "NUMERIC" => MapDecimalType(arg1, arg2),
                "MONEY" => DataTypeDescriptor.Decimal(19, 2),
                "REAL" => DataTypeDescriptor.Float(),
                "DOUBLE PRECISION" => DataTypeDescriptor.Double(),
                "UUID" => DataTypeDescriptor.Uuid(),
                "BYTEA" => DataTypeDescriptor.Blob(),
                "TIMESTAMP WITHOUT TIME ZONE" => DataTypeDescriptor.DateTime(arg1 ?? 6),
                "TIMESTAMP WITH TIME ZONE" => DataTypeDescriptor.DateTimeOffset(arg1 ?? 6),
                "DATE" => DataTypeDescriptor.Date(),
                "TIME WITHOUT TIME ZONE" => DataTypeDescriptor.Time(arg1 ?? 6),
                "BOOLEAN" => DataTypeDescriptor.Boolean(),
                _ => throw new NotSupportedException($"the data type '{storeType}' not supported.")
            };

            DataTypeDescriptor MapDecimalType(int? precision, int? scale)
            {
                // postgres support 1000 precision 
                if (precision == null || precision > 38)
                {
                    return DataTypeDescriptor.Decimal(38, 4);
                }
                return DataTypeDescriptor.Decimal(precision.Value, Convert.ToInt32(scale));

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

        public string ToDatabaseStoreType(DataTypeDescriptor dataType)
        {
            return dataType.DbType switch
            {
                CommonDataType.Boolean => "boolean",
                CommonDataType.Varchar => $"CHARACTER VARYING({dataType.Arg1})",
                CommonDataType.Char => $"CHARACTER({dataType.Arg1})",
                CommonDataType.NVarchar => $"CHARACTER VARYING({dataType.Arg1})",
                CommonDataType.NChar => $"CHARACTER({dataType.Arg1})",
                CommonDataType.Uuid => "uuid",
                CommonDataType.Float => "real",
                CommonDataType.Double => "float",
                CommonDataType.Binary => "bytea",
                CommonDataType.Int => "int",
                CommonDataType.SmallInt => "smallint",
                CommonDataType.BigInt => "bigint",
                CommonDataType.TinyInt => "smallint",
                CommonDataType.Text => "text",
                CommonDataType.NText => "text",
                CommonDataType.Varbinary => "bytea",
                CommonDataType.Blob => "bytea",
                CommonDataType.Decimal => $"numeric({dataType.Arg1},{dataType.Arg2})",
                CommonDataType.Date => "date",
                CommonDataType.Time => $"TIME({Math.Min(dataType?.Arg1 ?? 6, 6)}) WITHOUT TIME ZONE",
                CommonDataType.DateTime => $"TIMESTAMP({Math.Min(dataType?.Arg1 ?? 6, 6)}) WITHOUT TIME ZONE",
                CommonDataType.DateTimeOffset => $"TIMESTAMP({Math.Min(dataType?.Arg1 ?? 6, 6)}) WITH TIME ZONE",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
