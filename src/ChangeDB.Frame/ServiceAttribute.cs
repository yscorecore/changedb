using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ServiceAttribute : Attribute
    {
        public ServiceAttribute(Type serviceType)
        {
            this.ServiceType = serviceType;
        }
        public Type ServiceType { get; set; }
        public string Name { get; set; }
    }
}
