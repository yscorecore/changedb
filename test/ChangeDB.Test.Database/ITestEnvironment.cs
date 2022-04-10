using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public interface ITestEnvironment:IDisposable
    {

    }
    internal class EmptyTestEnvironment : ITestEnvironment
    {
        public void Dispose()
        {
        }
    }
}
