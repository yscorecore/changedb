using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace TestDB
{
    public static class Databases
    {
        private static readonly ConcurrentDictionary<string, IDatabaseInfrastructure> databaseInfrastructures = new(StringComparer.InvariantCultureIgnoreCase);
        public static void SetupDatabase<TInstance, TProvider>(string dbType, bool cached = true)
            where TInstance : IDatabaseInstance, new()
            where TProvider : IDatabaseServiceProvider, new()
        {
            var databaseInstance = new TInstance();
            var serviceProvider = new TProvider();
            SetupDatabase(dbType, databaseInstance, serviceProvider, cached);
        }

        public static void SetupDatabase(string dbType, IDatabaseInstance databaseInstance, IDatabaseServiceProvider databaseServiceProvider, bool cached = true)
        {
            if (databaseInfrastructures.ContainsKey(dbType))
            {
                throw new InvalidOperationException($"the database type '{dbType}' already exists.");
            }
            var allocator = new DatabaseAllocator(databaseInstance.ConnectionTemplate, databaseServiceProvider, cached);
            databaseInfrastructures.TryAdd(dbType,
                new DatabaseInfrastructure(databaseInstance, databaseServiceProvider, allocator));
        }

        public static void DisposeAll()
        {
            Parallel.ForEach(databaseInfrastructures.Values, p => p.Dispose());
            databaseInfrastructures.Clear();
        }

        public static IDatabaseInfrastructure GetDatabaseInfrastructure(string dbType)
        {
            _ = dbType ?? throw new ArgumentNullException(nameof(dbType));
            if (databaseInfrastructures.TryGetValue(dbType, out var infrastructure))
            {
                return infrastructure;
            }
            throw new NotSupportedException($"not supported database type '{dbType}'.");
        }
        public static IDatabase CreateDatabase(string dbType, bool readOnly, params string[] initsqls)
        {
            return GetDatabaseInfrastructure(dbType).Allocator.FromSqls(initsqls, readOnly);
        }
        public static IDatabase CreateDatabaseFromFile(string dbType, bool readOnly, string fileName)
        {
            return GetDatabaseInfrastructure(dbType).Allocator.FromScriptFile(fileName, readOnly);
        }
        public static IDatabase RequestDatabase(string dbType)
        {
            return GetDatabaseInfrastructure(dbType).Allocator.RequestDatabase();
        }

    }
}
