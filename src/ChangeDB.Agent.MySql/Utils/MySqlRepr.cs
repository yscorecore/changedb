using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ChangeDB.Migration;

namespace ChangeDB.Agent.MySql
{
    internal class MySqlRepr
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
            return $"'{input.Replace(ReplaceChars)}'";
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
                    return constant.ToString().TrimDecimalZeroTail();
                case Guid guid:
                    return $"UUID_TO_BIN('{guid}')";
                case byte[] bytes:
                    return $"UNHEX('{string.Join("", bytes.Select(p => p.ToString("X2")))}')";
                case TimeSpan timeSpan:
                    return $"'{timeSpan}'";
                case DateTime dateTime:
                    return $"'{FormatDateTime(dateTime)}'"; ;
                case DateTimeOffset dateTimeOffset:
                    return $"'{FormatDateTimeOffset(dateTimeOffset)}'";
                default:
                    return constant.ToString();
            }
        }

        private static string FormatDateTime(DateTime dateTime)
        {
            if (dateTime.TimeOfDay == TimeSpan.Zero)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }
            else if (dateTime.Millisecond == 0)
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff").TrimDecimalZeroTail();
            }
        }
        private static string FormatDateTimeOffset(DateTimeOffset dateTime)
        {
            if (dateTime.Millisecond == 0)
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss zzz");
            }
            else
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff").TrimDecimalZeroTail() + dateTime.ToString(" zzz");
            }
        }
    }

}
