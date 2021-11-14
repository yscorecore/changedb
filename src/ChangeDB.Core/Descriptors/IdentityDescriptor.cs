using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public class IdentityDescriptor : ExtensionObject
    {
        public virtual long? StartValue { get; set; }
        public virtual int? IncrementBy { get; set; }
        public virtual long? MinValue { get; set; }
        public virtual long? MaxValue { get; set; }
        public virtual bool? IsCyclic { get; set; }
    }
}
