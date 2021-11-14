using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDatabaseTypeMapper : IDatabaseTypeMapper
    {

        public static readonly PostgresDatabaseTypeMapper Default = new PostgresDatabaseTypeMapper();
        public DatabaseTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            return null;
            //var dataType = row.Field<string>("data_type");
            //var characterMaximumLength = row.Field<int?>("character_maximum_length");
            //var characterOctetLength = row.Field<int?>("character_octet_length");
            //var numericPrecision = row.Field<int?>("numeric_precision");
            //var numericPrecisionRadix = row.Field<int?>("numeric_precision_radix");
            //var numericScale = row.Field<int?>("numeric_scale");
            //var datetimePrecision = row.Field<int?>("datetime_precision");
            //return dataType?.ToUpperInvariant() switch
            //{
            //    "CHARACTER VARYING" => characterMaximumLength == null ? new DatabaseTypeDescriptor { DbType = CommonDatabaseType.NText } : new DatabaseTypeDescriptor { DbType = CommonDatabaseType.NVarchar, Length = characterMaximumLength },
            //    "CHARACTER" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.NChar, Length = characterMaximumLength },
            //    "TEXT" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.NText },
            //    "INTEGER" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Int },
            //    "BIGINT" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.BigInt },
            //    "SMALLINT" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.SmallInt },
            //    "TINYINT" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.TinyInt },
            //    "NUMERIC" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Decimal, Length = numericPrecision, Accuracy = numericScale },
            //    "MONEY" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Decimal, Length = 19, Accuracy = 2 },
            //    "REAL" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Float },
            //    "DOUBLE PRECISION" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Double },
            //    "UUID" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Uuid },
            //    "BYTEA" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Blob },
            //    "TIMESTAMP WITHOUT TIME ZONE" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.DateTime, Length = datetimePrecision },
            //    "TIMESTAMP WITH TIME ZONE" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.DateTimeOffset, Length = datetimePrecision },
            //    "DATE" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Date },
            //    "TIME WITHOUT TIME ZONE" => new DatabaseTypeDescriptor { DbType = CommonDatabaseType.Time, Length = datetimePrecision },
            //    _ => throw new NotSupportedException($"the data type '{dataType}' not supported.")
            //};
        }

        public string ToDatabaseStoreType(DatabaseTypeDescriptor dataType)
        {
            return dataType.DbType switch
            {
                CommonDatabaseType.Boolean => "bool",
                CommonDatabaseType.Varchar => $"varchar({dataType.Length})",
                CommonDatabaseType.Char => "char",
                CommonDatabaseType.NVarchar => $"varchar({dataType.Length})",
                CommonDatabaseType.NChar => "varchar",
                CommonDatabaseType.Uuid => "uuid",
                CommonDatabaseType.Float => "real",
                CommonDatabaseType.Double => "float",
                CommonDatabaseType.Binary => "bytea",
                CommonDatabaseType.Int => "int",
                CommonDatabaseType.SmallInt => "smallint",
                CommonDatabaseType.BigInt => "bigint",
                CommonDatabaseType.TinyInt => "smallint",
                CommonDatabaseType.Text => "text",
                CommonDatabaseType.NText => "text",
                CommonDatabaseType.Varbinary => "bytea",
                CommonDatabaseType.Blob => "bytea",
                CommonDatabaseType.Decimal => "numeric",
                CommonDatabaseType.Date => "date",
                CommonDatabaseType.Time => "TIME WITHOUT TIME ZONE",
                CommonDatabaseType.DateTime => "TIMESTAMP WITHOUT TIME ZONE",
                CommonDatabaseType.DateTimeOffset => "TIMESTAMP WITH TIME ZONE",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
