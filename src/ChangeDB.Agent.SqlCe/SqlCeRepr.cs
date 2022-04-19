using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeRepr
    {
        public static readonly SqlCeRepr Default = new SqlCeRepr();
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
                    return $"0x{string.Join("", bytes.Select(p => p.ToString("X2")))}";
                case DateTime dateTime:
                    return $"'{FormatDateTime(dateTime)}'"; ;
                case DateTimeOffset dateTimeOffset:
                    return $"'{FormatDateTimeOffset(dateTimeOffset)}'";
            }

            return constant.ToString();
        }

        public static string ReprString(string input, string storeType)
        {
            if (input is null) return null;
            List<string> items = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var ch in input)
            {
                if (char.IsControl(ch))
                {
                    AppendItem();
                    items.Add($"NCHAR({(int)ch})");
                }
                else
                {
                    stringBuilder.Append(ch);
                }
            }

            AppendItem();

            return string.Join(" + ", items);

            void AppendItem()
            {
                if (stringBuilder.Length > 0)
                {
                    items.Add(EncodeItem(stringBuilder.ToString()));

                    stringBuilder.Clear();
                }
            }
            string EncodeItem(string text)
            {
                return $"'{text.Replace("'", "''")}'";
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
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff").TrimDecimalZeroTail();
            }
        }
        private static string FormatDateTimeOffset(DateTimeOffset dateTime)
        {
            if (dateTime.Millisecond == 0)
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff").TrimDecimalZeroTail();
            }
        }
    }
}
