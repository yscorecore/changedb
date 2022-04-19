using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChangeDB
{
    public static class StringExtensions
    {
        public static int FixedHash(this string str)
        {
            if (str == null) return default;
            unchecked
            {
                return str.Aggregate(17, (a, b) => 23 * a + b);
            }
        }

        public static string Replace(this string str, IDictionary<string, string> dict)
        {
            if (str is null) return default;
            StringBuilder sb = new StringBuilder(str);
            return sb.Replace(dict).ToString();
        }

        private static StringBuilder Replace(this StringBuilder sb,
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
        private static readonly Regex trimDecimalRegex = new(@"(?<int>\d?)\.(?<dec>\d*)$");
        public static string TrimDecimalZeroTail(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return trimDecimalRegex.Replace(str,
                (m) =>
                {
                    var intStr = m.Groups["int"].Value;
                    var decStr = m.Groups["dec"].Value;
                    var trimedDecStr = decStr.TrimEnd('0');
                    if (string.IsNullOrEmpty(intStr))
                    {
                        if (string.IsNullOrEmpty(decStr))
                        {
                            return m.Value;
                        }
                        return string.IsNullOrEmpty(trimedDecStr) ? ".0" : $".{trimedDecStr}";
                    }
                    else
                    {
                        return string.IsNullOrEmpty(trimedDecStr) ? $"{intStr}" : $"{intStr}.{trimedDecStr}";
                    }
                });
        }
    }
}
