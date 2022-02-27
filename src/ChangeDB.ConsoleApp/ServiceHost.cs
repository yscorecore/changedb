using System;
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
