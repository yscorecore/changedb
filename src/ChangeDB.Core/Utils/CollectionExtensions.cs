using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public static class CollectionExtensions
    {
        public static IEnumerable<T> Foreach<T>(this IEnumerable<T> source, Action<T> action)
            where T : class
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            foreach (var item in source)
            {
                action?.Invoke(item);
            }
            return source;
        }

    }
}
