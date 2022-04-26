using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ChangeDB.Dump;

namespace ChangeDB.Migration
{
    public interface IDataDumper
    {
        Task WriteTables(IAsyncEnumerable<DataTable> datas, TableDescriptor tableDescriptor, DumpContext dumpContext);
    }
}
