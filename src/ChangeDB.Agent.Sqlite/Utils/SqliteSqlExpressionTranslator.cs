using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Sqlite
{
    internal class SqliteSqlExpressionTranslator
    {
        public static readonly SqliteSqlExpressionTranslator Default = new SqliteSqlExpressionTranslator();

        private static readonly ConcurrentDictionary<string, object> ValueCache =
            new ConcurrentDictionary<string, object>();


        public SqlExpressionDescriptor ToCommonSqlExpression(string defaultValue, string storeType, IDbConnection conn)
        {
            if (defaultValue is null)
            {
                return default;
            }
            return defaultValue.ToLowerInvariant() switch
            {
                "current_date" or "current_time" or "current_timestamp" => new SqlExpressionDescriptor { Function = Function.Now },
                "randomblob(16)" => new SqlExpressionDescriptor { Function = Function.Uuid },
                _ => new SqlExpressionDescriptor { Constant = GetDefaultValue(defaultValue, storeType, conn) }
            };
        }


        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, string storeType, DataTypeDescriptor dataTypeDescriptor)
        {
            if (sqlExpression?.Function != null)
            {
                return sqlExpression.Function.Value switch
                {
                    Function.Now => GetNow(),
                    Function.Uuid => "randomblob(16)",
                    _ => throw new NotSupportedException($"not supported function {sqlExpression.Function.Value}")
                };
            }
            return SqliteRepr.ReprConstant(sqlExpression?.Constant, storeType);

            string GetNow()
            {
                return dataTypeDescriptor.DbType.ToString().ToLowerInvariant() switch
                {
                    "timestamp" or "datetime" or "smalldatetime" or "datetime2" or "datetimeoffset" or "timestamp with time zone" or "timestamp without time zone" => "CURRENT_TIMESTAMP",
                    "date" => "CURRENT_DATE",
                    "time" or "time without time zone" => "CURRENT_TIME",
                    _ => storeType,
                };
            }
        }

        private static string FormatDefaultValue(CommonDataType type, string defaultValue)
        {
            switch (type)
            {
                case CommonDataType.Int:
                case CommonDataType.SmallInt:
                case CommonDataType.BigInt:
                case CommonDataType.TinyInt:
                case CommonDataType.Boolean:
                    return defaultValue.Trim('\'');
                default:
                    return defaultValue;
            }
        }

        private static object FormatObjectValue(CommonDataType type, IDbConnection conn, string formattedValue)
        {
            var objectValue = conn.ExecuteScalar($"SELECT {formattedValue}");
            switch (type)
            {
                case CommonDataType.Text:
                case CommonDataType.NText:
                case CommonDataType.Char:
                case CommonDataType.NChar:
                case CommonDataType.Varchar:
                case CommonDataType.NVarchar:
                    return objectValue.ToString();
                case CommonDataType.Boolean:
                    return BooleanParse(objectValue);
                case CommonDataType.Float:
                    return Convert.ToSingle(objectValue);
                case CommonDataType.Uuid:
                    return GuidParse(objectValue.ToString());
                case CommonDataType.Date:
                case CommonDataType.DateTime:
                    return DateTime.Parse(formattedValue.Trim('\''));
                case CommonDataType.DateTimeOffset:
                    return DateTimeOffset.Parse(formattedValue.Trim('\''));
                default:
                    return objectValue;
            }
        }

        private static object GetDefaultValue(string defaultValue, string storeType, IDbConnection conn)
        {
            var type = SqliteDataTypeMapper.Default.ToCommonDatabaseType(storeType);
            string formattedValue = FormatDefaultValue(type.DbType, defaultValue);
            return FormatObjectValue(type.DbType, conn, formattedValue);
        }

        private static bool BooleanParse(object o)
        {
            if (o == null) return false;

            string value = o.ToString();
            if (value == "1") return true;
            if ("true".Equals(value, StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }

        private static byte[] BytesParse(string defaultValue)
        {
            if (!IsHexString(defaultValue))
            {
                throw new ArgumentException($"'{default}' is not a hex string");
            }
            string hex = defaultValue.Substring(2, defaultValue.Length - 4);
            return Enumerable
              .Range(0, hex.Length / 2)
              .Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
              .ToArray();
        }

        private static bool IsHexString(string s)
        {
            if (s.Length < 3) return false;
            if (s[0] != 'x' && s[0] != 'X') return false;
            if (s[1] != '\'' && s[^1] != '\'') return false;
            return true;
        }

        private static Guid GuidParse(string s)
        {
            try
            {
                if (IsHexString(s))
                {
                    return new Guid(BytesParse(s));
                }
                return Guid.Parse(s.Trim('\''));
            }
            catch (Exception e)
            {
                throw new ArgumentException($"'{s}' is not a hex string or uuid string", e);
            }
        }
    }
}
