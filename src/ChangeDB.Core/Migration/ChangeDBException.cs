using System;

namespace ChangeDB.Migration
{

    public class ChangeDBException : ApplicationException
    {
        public ChangeDBException(string message) : base(message) { }
        public ChangeDBException(string message, Exception inner) : base(message, inner) { }
    }

}
