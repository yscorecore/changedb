using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using ChangeDB.Migration;

namespace ChangeDB.Agent
{

    public abstract class BaseAgent : IAgent
    {
        protected BaseAgent()
        {
            var assembly = this.GetType().Assembly;
            this.ServiceProvider = new AssemblyServiceProvider(assembly);
        }

        private IServiceProvider ServiceProvider { get; }
        public virtual IConnectionProvider ConnectionProvider => GetService<IConnectionProvider>();
        public virtual IDataMigrator DataMigrator => GetService<IDataMigrator>();
        public virtual IMetadataMigrator MetadataMigrator => GetService<IMetadataMigrator>();
        public virtual IDatabaseManager DatabaseManger => GetService<IDatabaseManager>();
        public abstract AgentSetting AgentSetting { get; }
        public virtual IDataDumper DataDumper => GetService<IDataDumper>();

        private T GetService<T>()
        {
            return (T)GetServiceInternal(typeof(T));
        }

        private readonly ConcurrentDictionary<Type, object> _caches = new();
        private object GetServiceInternal(Type type)
        {
            return _caches.GetOrAdd(type, t => ServiceProvider.GetService(t));
        }

        class AssemblyServiceProvider : IServiceProvider
        {
            private readonly Assembly _assembly;
            public AssemblyServiceProvider(Assembly assembly)
            {
                _assembly = assembly;
            }
            public object? GetService(Type serviceType)
            {
                var implType = _assembly.GetTypes()
                    .FirstOrDefault(p => !p.IsAbstract && p.GetConstructor(Type.EmptyTypes) != null && serviceType.IsAssignableFrom(p));
                if (implType != null)
                {
                    return Activator.CreateInstance(implType);
                }
                else
                {
                    throw new ChangeDBException(
                        $"can not found impl service of the type '{serviceType.FullName}' in assembly '{_assembly.FullName}'");
                }
            }
        }
    }
}
