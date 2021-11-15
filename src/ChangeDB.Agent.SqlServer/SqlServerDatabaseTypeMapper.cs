using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDatabaseTypeMapper:IDatabaseTypeMapper
    {
        public static IDatabaseTypeMapper Default = new SqlServerDatabaseTypeMapper();
        public DatabaseTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            throw new System.NotImplementedException();
        }

        public string ToDatabaseStoreType(DatabaseTypeDescriptor commonDatabaseType)
        {
            throw new System.NotImplementedException();
        }
    }
}
