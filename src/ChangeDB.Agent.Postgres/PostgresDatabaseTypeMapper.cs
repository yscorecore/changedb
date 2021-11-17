using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
            _ = storeType ?? throw new ArgumentNullException(nameof(storeType));
            var (type, arg1, arg2) = ParseStoreType(storeType);
            return type.ToUpperInvariant() switch
            {
                "CHARACTER VARYING" =>arg1==null?DatabaseTypeDescriptor.NText():  DatabaseTypeDescriptor.NVarchar(arg1.Value),
                "CHARACTER" => DatabaseTypeDescriptor.NChar(arg1.Value),
                "TEXT" => DatabaseTypeDescriptor.NText(),
                "INTEGER" => DatabaseTypeDescriptor.Int(),
                "BIGINT" =>DatabaseTypeDescriptor.BigInt(),
                "SMALLINT" => DatabaseTypeDescriptor.SmallInt(),
                "TINYINT" => DatabaseTypeDescriptor.SmallInt(),
                "NUMERIC" =>MapDecimalType(arg1,arg2),
                "MONEY" =>DatabaseTypeDescriptor.Decimal(19,2),
                "REAL" => DatabaseTypeDescriptor.Float(),
                "DOUBLE PRECISION" =>DatabaseTypeDescriptor.Double(),
                "UUID" =>DatabaseTypeDescriptor.Uuid(),
                "BYTEA" => DatabaseTypeDescriptor.Blob(),
                "TIMESTAMP WITHOUT TIME ZONE" => DatabaseTypeDescriptor.DateTime(arg1 ?? 6),
                "TIMESTAMP WITH TIME ZONE" => DatabaseTypeDescriptor.DateTimeOffset(arg1 ?? 6),
                "DATE" =>DatabaseTypeDescriptor.Date(),
                "TIME WITHOUT TIME ZONE" =>DatabaseTypeDescriptor.Time(arg1 ?? 6),
                _ => throw new NotSupportedException($"the data type '{storeType}' not supported.")
            };

            DatabaseTypeDescriptor MapDecimalType(int? precision, int? scale)
            {
                // postgres support 1000 precision 
                if (precision ==null || precision>38)
                {
                    return DatabaseTypeDescriptor.Decimal(38, 4);
                }
                return DatabaseTypeDescriptor.Decimal(precision.Value,Convert.ToInt32(scale));
                
            }
        }

        private static (string Type, int? Arg1, int? Arg2) ParseStoreType(string storeType)
        {
            var index1 = storeType.IndexOf('(');
            var index2 = storeType.IndexOf(')');
            if (index1 > 0 && index2 > 0)
            {
                var type = storeType[..index1] + storeType.Substring(index2+1);
                var index3 = storeType.IndexOf(',', index1);
                if (index3 > 0)
                {
                    return (type, int.Parse(storeType.Substring(index1+1,index3-index1-1).Trim()), 
                        int.Parse(storeType.Substring(index3+1,index2-index3-1).Trim()));
                }
                else
                {
                    return (type, int.Parse(storeType.Substring(index1+1,index2-index1-1).Trim()), null);
                }
            }

            return (storeType.ToLower(),null,null);
        }

        public string ToDatabaseStoreType(DatabaseTypeDescriptor dataType)
        {
            return dataType.DbType switch
            {
                CommonDatabaseType.Boolean => "boolean",
                CommonDatabaseType.Varchar => $"varchar({dataType.Arg1})",
                CommonDatabaseType.Char => $"char({dataType.Arg1})",
                CommonDatabaseType.NVarchar => $"varchar({dataType.Arg1})",
                CommonDatabaseType.NChar => $"char({dataType.Arg1})",
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
                CommonDatabaseType.Decimal => $"numeric({dataType.Arg1},{dataType.Arg2})",
                CommonDatabaseType.Date => "date",
                CommonDatabaseType.Time => $"TIME({dataType.Arg1}) WITHOUT TIME ZONE",
                CommonDatabaseType.DateTime => $"TIMESTAMP({dataType.Arg1}) WITHOUT TIME ZONE",
                CommonDatabaseType.DateTimeOffset => $"TIMESTAMP({dataType.Arg1}) WITH TIME ZONE",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
