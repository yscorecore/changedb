using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{

    public class ChangeDBException : ApplicationException
    {
        public ChangeDBException(string message) : base(message) { }
        public ChangeDBException(string message, Exception inner) : base(message, inner) { }
    }

}
