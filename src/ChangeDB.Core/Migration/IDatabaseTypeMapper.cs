using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDatabaseTypeMapper
    {
        DatabaseTypeDescriptor ToCommonDatabaseType(string storeType);

        string ToDatabaseStoreType(DatabaseTypeDescriptor commonDatabaseType);
    }
}
