using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IRepr
    {
        string ReprValue(object value);
    }
}
