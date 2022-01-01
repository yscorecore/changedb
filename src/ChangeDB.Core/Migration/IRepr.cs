using System.Data;

namespace ChangeDB.Migration
{
    public interface IRepr
    {
        string ReprValue(object value, string storeType);
        string ReprValue(object value, DbType dbType);
    }
}
