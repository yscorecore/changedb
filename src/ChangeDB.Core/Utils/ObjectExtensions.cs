using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChangeDB
{
    public static class ObjectExtensions
    {
        public static T DeepClone<T>(this T source)
        {
            var text = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(text);
        }

        public static T DoIfNotNull<T>(this T source, Action<T> action)
            where T : class
        {
            if (source != null && action != null)
            {
                action(source);

            }
            return source;
        }
        public static T DeepCloneAndSet<T>(this T soruce, Action<T> action)
            where T : class
        {
            return soruce.DeepClone().DoIfNotNull(action);
        }
    }
}
