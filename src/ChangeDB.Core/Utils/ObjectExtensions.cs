using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Xml.Serialization;

namespace ChangeDB
{
    public static class ObjectExtensions
    {
        public static T DeepClone<T>(this T source)
        {
            if (source == null) return default(T);

            //using (var ms = new MemoryStream())
            //{
            //    BinaryFormatter bf = new BinaryFormatter();
            //    bf.Serialize(ms, source);
            //    ms.Seek(0, SeekOrigin.Begin);
            //    return (T)bf.Deserialize(ms);
            //}
            Type objType = source.GetType();
            XmlSerializer tXmlSerializer = new XmlSerializer(objType);
            using (MemoryStream ms = new MemoryStream(1024))
            {
                tXmlSerializer.Serialize(ms, source);
                ms.Seek(0, SeekOrigin.Begin);
                return (T)tXmlSerializer.Deserialize(ms);
            }
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
