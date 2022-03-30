using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Xml.Serialization;
using BindingFlags = System.Reflection.BindingFlags;

namespace ChangeDB
{
    public static class ObjectExtensions
    {
        public static T DeepClone<T>(this T source)
        {
            if (source == null) return default;
            return (T)DeepCloneInternal(source);
        }

        private static object DeepCloneInternal(object val)
        {
            if (val == null) return null;
            var type = val.GetType();

            if (type.IsValueType)
            {
                return val;
            }

            if (type.IsArray && val is Array arr)
            {
                var itemType = type.GetElementType();
                var newArray = Array.CreateInstance(itemType!, arr.Length);
                for (int i = 0; i < arr.Length; i++)
                {
                    newArray.SetValue(DeepCloneInternal(arr.GetValue(i)), i);
                }

                return newArray;
            }

            bool hasEmptyCtor = type.GetConstructor(Type.EmptyTypes) != null;
            if (val is IDictionary dic && hasEmptyCtor)
            {
                var newDic = Activator.CreateInstance(type) as IDictionary;
                foreach (var key in dic.Keys)
                {
                    newDic!.Add(DeepCloneInternal(key), DeepCloneInternal(dic[key]));
                }

                return newDic;
            }

            if (val is IList list && hasEmptyCtor)
            {
                var newList = Activator.CreateInstance(type) as IList;
                foreach (var item in list)
                {
                    newList!.Add(DeepCloneInternal(item));
                }

                return newList;
            }

            if (hasEmptyCtor)
            {
                var newObj = Activator.CreateInstance(type);
                foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead && p.CanWrite))
                {
                    prop.SetValue(newObj, DeepCloneInternal(prop.GetValue(val)));
                }
                return newObj;
            }
            return val;
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
