using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDatabaseTypeMapper : IDatabaseTypeMapper
    {

        public static readonly SqlCeDatabaseTypeMapper Default = new SqlCeDatabaseTypeMapper();
        public DatabaseTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            throw new NotImplementedException();

        }

        public string ToDatabaseStoreType(DatabaseTypeDescriptor dataType)
        {
            throw new NotImplementedException();
        }
    }
}
