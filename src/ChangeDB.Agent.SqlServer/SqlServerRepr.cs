using System;
using System.Data;
using System.Linq;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerRepr : IRepr
    {
        public static readonly IRepr Default = new SqlServerRepr();
        public string ReprValue(object value, string storeType)
        {
            return ReprConstant(value);
        }

        public string ReprValue(object value, DbType dbType)
        {
            return ReprConstant(value);
        }

        public static string ReprConstant(object constant)
        {
            if (constant == null || Convert.IsDBNull(constant))
            {
                return "null";
            }

            switch (constant)
            {
                case string str:
                    return ReprString(str);
                case bool:
                    return Convert.ToInt32(constant).ToString();
                case double or float or long or int or short or char or byte or decimal:
                    return constant.ToString();
                case Guid guid:
                    return $"'{guid}'";
                case byte[] bytes:
                    return $"0x{string.Join("", bytes.Select(p => p.ToString("X2")))}";
                case DateTime dateTime:
                    return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'"; ;
                case DateTimeOffset dateTimeOffset:
                    return $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss zzz}'";
            }

            return constant.ToString();
        }

        public static string ReprString(string input)
        {
            if (input is null) return null;
            return $"'{input.Replace("'", "''")}'";
        }
    }
}
