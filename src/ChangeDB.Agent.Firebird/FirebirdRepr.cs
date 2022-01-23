using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Firebird
{
    public class FirebirdRepr : IRepr
    {
        public static readonly IRepr Default = new FirebirdRepr();
        public string ReprValue(object value)
        {
            return ReprConstant(value);
        }

        public static string ReprString(string input)
        {
            if (input is null) return null;
            var items = new List<string>();
            var stringBuilder = new StringBuilder();
            foreach (var ch in input)
            {
                if (char.IsControl(ch) && ch<128)
                {
                    AddCachedToItems();
                    items.Add($"ASCII_CHAR({Convert.ToInt32(ch)})");
                }
                else
                { 
                    stringBuilder.Append(ch);
                }
            }
            AddCachedToItems();

            return string.Join("||", items);
          
            void AddCachedToItems()
            {
                if (stringBuilder.Length > 0)
                {
                    items.Add(ReprStringItem(stringBuilder.ToString()));
                    stringBuilder.Clear();
                }
            }
            string ReprStringItem(string item) => $"'{item.Replace("'", "''")}'";
        }

        public static string ReprConstant(object constant)
        {
            if (constant == null || Convert.IsDBNull(constant))
            {
                return "null";
            }

            return constant switch
            {
                string str => ReprString(str),
                bool => Convert.ToBoolean(constant).ToString().ToLowerInvariant(),
                double or float or long or int or short or char or byte or decimal or bool => constant.ToString(),
                Guid guid => $"x'{guid:N}'",
                byte[] bytes => $"x'{string.Join(string.Empty, bytes.Select(p => p.ToString("X2")))}'",
                TimeSpan timeSpan => $"'{timeSpan}'",
                DateTime dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'",
                DateTimeOffset dateTimeOffset => $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss.fff zzz}'",
                _ => constant.ToString(),
            };
        }
    }

}