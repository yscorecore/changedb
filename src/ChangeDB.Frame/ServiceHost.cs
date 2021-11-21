using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
namespace ChangeDB
{
    public class ServiceHost
    {
        public readonly static ServiceHost Default = new ServiceHost();

        private ServiceCollection serviceCollection = new ServiceCollection();
        public ServiceHost()
        {
            serviceCollection = new ServiceCollection();
            AddServices(serviceCollection);
            AddDictionary(ServiceCollection);

        }

        public ServiceCollection ServiceCollection { get => serviceCollection; set => serviceCollection = value; }

        public IServiceProvider BuildServiceProvider()
        {
            return serviceCollection.BuildServiceProvider();
        }
        private static void AddServices(ServiceCollection serviceCollection)
        {
            var services = AppDomain.CurrentDomain.GetAssemblies().SelectMany(p => p.GetTypes())
                .Where(p => Attribute.IsDefined(p, typeof(ServiceAttribute)))
                .Select(p => new { Type = p, Attr = p.GetCustomAttribute<ServiceAttribute>() });
            foreach (var service in services)
            {
                serviceCollection.AddSingleton(service.Attr.ServiceType, service.Type);
            }
        }

        private static void AddDictionary(ServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(IDictionary<,>), typeof(InjectionDictionary<,>));
        }

        protected class InjectionDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public InjectionDictionary(IEnumerable<TValue> items)
            {
                if (items == null)
                {
                    throw new ArgumentNullException(nameof(items));
                }
                if (typeof(TKey) != typeof(string))
                {
                    throw new InvalidOperationException("The key type of IDictionary<,> must be string.");
                }

                foreach (var item in items)
                {
                    if (item == null) continue;
                    TKey key = (TKey)(object)GetServiceKey(item.GetType());
                    if (this.ContainsKey(key))
                    {
                        throw new InvalidOperationException($"The key '{key}' has exists.");
                    }
                    this[key] = item;
                }
            }

            private static string GetServiceKey(Type type)
            {
                var serviceAttribute = type.GetCustomAttribute<ServiceAttribute>();
                if (serviceAttribute != null && !string.IsNullOrEmpty(serviceAttribute.Name))
                {
                    return serviceAttribute.Name;
                }
                else
                {
                    return type.FullName;
                }
            }
        }
    }
}
