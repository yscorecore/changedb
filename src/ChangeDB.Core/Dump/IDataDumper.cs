using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;

namespace ChangeDB.Migration
{
    public interface IDataDumper
    {
        Task WriteTable(DataTable data, TableDescriptor table, DumpContext dumpContext);
    }
}
