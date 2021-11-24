using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Descriptors
{
    public class SqlExpressionDescriptor : ExtensionObject
    {
        public Function? Function { get; set; }

        public object Constant { get; set; }
    }
    public enum Function
    {
        Now,
        Uuid,
    }
}
