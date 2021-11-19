using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDataTypeMapper
    {
        DataTypeDescriptor ToCommonDatabaseType(string storeType);

        string ToDatabaseStoreType(DataTypeDescriptor commonDataType);
    }
}
