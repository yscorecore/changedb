using System.Linq;

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
    }
}
