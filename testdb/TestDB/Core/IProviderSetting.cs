using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface IProviderSetting
    {
        bool SupportFastClone { get; }

        long FastClonedMinimalSqlSize { get; }
    }
}
