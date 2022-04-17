using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestDB.Core;

namespace TestDB
{

    public class DatabaseInfrastructure : IDatabaseInfrastructure
    {
        public DatabaseInfrastructure(IDatabaseInstance instance, IDatabaseServiceProvider service, IDatabaseAllocator allocator)
        {
            Instance = instance;
            Service = service;
            Allocator = allocator;
        }
        public IDatabaseAllocator Allocator { get; }
        public IDatabaseInstance Instance { get; }
        public IDatabaseServiceProvider Service { get; }

        public void Dispose()
        {
            Allocator.Dispose();
            Instance.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await Allocator.DisposeAsync();
            await Instance.DisposeAsync();
        }
    }
}
