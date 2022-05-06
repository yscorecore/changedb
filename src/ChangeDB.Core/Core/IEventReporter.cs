using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB
{
    public interface IEventReporter
    {
        // void RaiseObjectCreated(ObjectInfo objectInfo);
        // void RaiseTableDataMigrated(TableDataInfo tableDataInfo);
        // void RaiseWarning(object warning);
        void RaiseEvent<T>(T eventInfo) where T : IEventInfo;
    }

    public interface IEventInfo
    {
    }
}
