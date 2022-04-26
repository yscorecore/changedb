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
        void RaiseObjectCreated(ObjectInfo objectInfo);
    }
}
