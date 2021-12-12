using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp
{
    internal class ServiceHost : IServiceProvider
    {
        public static ServiceHost Default = new ServiceHost();
        public ServiceHost()
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddChangeDb();
            serviceProvider = sc.BuildServiceProvider();

        }
        private IServiceProvider serviceProvider;
        public object GetService(Type serviceType)
        {
            return serviceProvider;
        }
    }
}
