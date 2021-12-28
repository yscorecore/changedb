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
        public static readonly ServiceHost Default = new ServiceHost();

        private ServiceHost()
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddChangeDb();
            _serviceProvider = sc.BuildServiceProvider();

        }
        private readonly IServiceProvider _serviceProvider;
        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }
    }
}
