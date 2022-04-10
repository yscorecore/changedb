using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ChangeDB.Migration;

namespace ChangeDB.Agent.MySql
{
    public class MySqlRepr
    {
        public static readonly MySqlRepr Default = new MySqlRepr();

        // https://dev.mysql.com/doc/refman/8.0/en/string-literals.html
        private static readonly Dictionary<string, string> ReplaceChars = new Dictionary<string, string>()
        {
            ["\\"] = @"\\",
            ["\n"] = @"\n",
            ["\r"] = @"\r",
            ["\t"] = @"\t",
            ["\b"] = @"\b",
            ["\'"] = @"\'",
        };

        public string ReprValue(object value, string storeType)
        {
            return ReprValue(value);
        }

        public string ReprValue(object value, DbType dbType)
        {
            return ReprValue(value);
        }

        public string ReprValue(object value)
        {
            return ReprConstant(value);
        }

        public static string ReprString(string input)
        {
            if (input is null) return null;
            return $"'{Replace(input, ReplaceChars)}'";
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
                    return Convert.ToBoolean(constant).ToString().ToLowerInvariant();
                case double or float or long or int or short or char or byte or decimal or bool:
                    return constant.ToString();
                case Guid guid:
                    return $"UUID_TO_BIN('{guid}')";
                case byte[] bytes:
                    return $"UNHEX('{string.Join("", bytes.Select(p => p.ToString("X2")))}')";
                case TimeSpan timeSpan:
                    return $"'{timeSpan}'";
                case DateTime dateTime:
                    return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'"; ;
                case DateTimeOffset dateTimeOffset:
                    return $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss zzz}'";
                default:
                    return constant.ToString();
            }
        }


        private static string Replace(string str, IDictionary<string, string> dict)
        {
            if (str is null) return default;
            StringBuilder sb = new StringBuilder(str);
            return Replace(sb, dict).ToString();
        }

        private static StringBuilder Replace(StringBuilder sb,
            IDictionary<string, string> dict)
        {
            if (dict == null)
            {
                return sb;
            }

            foreach (var (key, value) in dict)
            {
                sb.Replace(key, value);
            }
            return sb;
        }


    }

}
