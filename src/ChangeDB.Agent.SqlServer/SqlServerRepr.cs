using System;
using System.Linq;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerRepr : IRepr
    {
        public static readonly IRepr Default = new SqlServerRepr();
        public string ReprValue(object value)
        {
            return ReprConstant(value);
        }

        public static string ReprConstant(object constant)
        {
            if (constant == null || Convert.IsDBNull(constant))
            {
                return "null";
            }
            else if (constant is string str)
            {
                return ReprString(str);
            }
            else if (constant is bool)
            {
                return Convert.ToInt32(constant).ToString();
            }
            else if (constant is double || constant is float || constant is long || constant is int ||
                     constant is short || constant is char || constant is byte || constant is decimal)
            {
                return constant.ToString();
            }
            else if (constant is Guid guid)
            {
                return $"'{guid}'";
            }
            else if (constant is byte[] bytes)
            {
                return $"0x{string.Join("", bytes.Select(p => p.ToString("X2")))}";
            }
            else if (constant is DateTime dateTime)
            {
                return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'"; ;
            }
            else if (constant is DateTimeOffset dateTimeOffset)
            {
                return $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss zzz}'";
            }
            else
            {
                return constant.ToString();
            }
        }

        public static string ReprString(string input)
        {
            if (input is null) return null;
            return $"'{input.Replace("'", "''")}'";
        }
    }
}
