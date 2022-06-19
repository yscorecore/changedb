using System;
using System.Data;
using System.Linq;

namespace ChangeDB.Agent.Sqlite
{
    internal class SqliteRepr
    {
        public static readonly SqliteRepr Default = new SqliteRepr();

        public static string ReprConstant(object constant, string storeType)
        {
            if (constant == null || Convert.IsDBNull(constant))
            {
                return "null";
            }

            switch (constant)
            {
                case string str:
                    return ReprString(str, storeType);
                case bool:
                    return Convert.ToInt32(constant).ToString();
                case double or float or long or int or short or char or byte or decimal:
                    return constant.ToString().TrimDecimalZeroTail();
                case Guid guid:
                    return $"'{guid}'";
                case byte[] bytes:
                    return $"x'{string.Join("", bytes.Select(p => p.ToString("X2")))}'";
                case DateTime dateTime:
                    return $"'{FormatDateTime(dateTime)}'"; ;
                case DateTimeOffset dateTimeOffset:
                    return $"'{FormatDateTimeOffset(dateTimeOffset)}'";
            }

            return constant.ToString();
        }

        public static string ReprString(string input, string storeType)
        {
            return $"'{input.Replace("'", "''")}'";
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
