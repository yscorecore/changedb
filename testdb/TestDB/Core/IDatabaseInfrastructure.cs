using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface IDatabaseInfrastructure : IDisposable, IAsyncDisposable
    {
        IDatabaseAllocator Allocator { get; }
        IDatabaseInstance Instance { get; }
        IDatabaseServiceProvider Service { get; }
    }
}
