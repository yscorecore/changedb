using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface IDatabaseServiceProvider: IConnectionProvider, ISqlSpliter, IProviderSetting,IDatabaseManager
    {
    }
}
