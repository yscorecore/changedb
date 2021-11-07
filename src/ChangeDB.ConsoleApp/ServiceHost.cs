using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.ConsoleApp
{
    class ServiceHost:YS.Knife.Hosting.KnifeHost
    {
        public readonly static ServiceHost Default = new ServiceHost();
    }
}
