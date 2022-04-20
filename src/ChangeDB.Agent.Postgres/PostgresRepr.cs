using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    internal class PostgresRepr
    {
        public static readonly PostgresRepr Default = new PostgresRepr();

        // https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-STRINGS-ESCAPE
        private static readonly Dictionary<string, string> ReplaceChars = new Dictionary<string, string>()
        {
            ["\\"] = @"\\",
            ["\n"] = @"\n",
            ["\r"] = @"\r",
            ["\t"] = @"\t",
            ["\b"] = @"\b",
            ["\f"] = @"\f",
        };
        public string ReprValue(object value, string storeType)
        {
            return ReprConstant(value);
        }

        public string ReprValue(object value, DbType dbType)
        {
            return ReprConstant(value);

        }

        public static string ReprString(string input)
        {
            if (input is null) return null;
            var replaced = input.Replace(ReplaceChars);
            return replaced == input ? $"'{ReplaceSingleQuote(replaced)}'" : $"E'{ReplaceSingleQuote(replaced)}'";

            static string ReplaceSingleQuote(string text)
            {
                return text.Replace("\'", "\'\'");
            }
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
                    return $"'{guid}'";
                case byte[] bytes:
                    return $"'\\x{string.Join("", bytes.Select(p => p.ToString("X2")))}'";
                case DateTime dateTime:
                    return $"'{FormatDateTime(dateTime)}'"; ;
                case DateTimeOffset dateTimeOffset:
                    return $"'{FormatDateTimeOffset(dateTimeOffset)}'";
                default:
                    return constant.ToString();
            }
        }

        private static string ReprCopyString(string input)
        {
            if (input is null) return null;
            return input.Replace(ReplaceChars);
        }

        public static string ReprCopyConstant(object constant)
        {
            if (constant == null || Convert.IsDBNull(constant))
            {
                return "\\N";
            }
            return constant switch
            {
                string str => ReprCopyString(str),
                bool => Convert.ToBoolean(constant).ToString().ToLowerInvariant(),
                double or float or long or int or short or char or byte or decimal or bool => constant.ToString().TrimDecimalZeroTail(),
                Guid guid => $"{guid}",
                byte[] bytes => $"\\\\x{string.Join("", bytes.Select(p => p.ToString("X2")))}",
                DateTime dateTime => $"{FormatDateTime(dateTime)}",
                DateTimeOffset dateTimeOffset => $"{FormatDateTimeOffset(dateTimeOffset)}",
                _ => constant.ToString(),
            };
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
