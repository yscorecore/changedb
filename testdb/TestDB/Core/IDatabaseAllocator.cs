using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface IDatabaseAllocator : IDisposable, IAsyncDisposable
    {
        IDatabase FromScriptFile(string sqlScriptFile, bool readOnly = true);
        IDatabase FromSqls(IEnumerable<string> initSqls, bool readOnly = true);
        IDatabase RequestDatabase();

    }
}
