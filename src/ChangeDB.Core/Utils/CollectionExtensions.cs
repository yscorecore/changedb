using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public static async IAsyncEnumerable<TResult> ToAsync<T, TResult>(this IEnumerable<T> enumerable,
    Func<T, Task<TResult>> selector)
        {
            foreach (var item in enumerable)
            {
                yield return await selector(item);
            }
        }
        public static IAsyncEnumerable<T> ToAsync<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ToAsync(item => Task.FromResult(item));
        }
        public static Func<int> NewSequence(int start = 0) => () => start++;
    }
}
