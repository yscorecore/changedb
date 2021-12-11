using System;
using System.Collections.Generic;

namespace ChangeDB
{
    public static class CollectionExtensions
    {
        public static IEnumerable<T> Each<T>(this IEnumerable<T> source, Action<T> action)
            where T : class
        {
            if (source != null)
            {
                foreach (var item in source)
                {
                    action?.Invoke(item);
                }
            }
            return source;
        }
        public static IEnumerable<T> Each<T>(this IEnumerable<T> source, Action<T, int> action)
            where T : class
        {
            if (source != null)
            {
                var sequence = NewSequence();
                foreach (var item in source)
                {
                    action?.Invoke(item, sequence());
                }
            }
            return source;
        }
        public static Func<int> NewSequence(int start = 0) => () => start++;
    }
}
