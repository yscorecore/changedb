namespace ChangeDB.Migration
{
    public interface IDataTypeMapper
    {
        DataTypeDescriptor ToCommonDatabaseType(string storeType);

        string ToDatabaseStoreType(DataTypeDescriptor commonDataType);
    }
}
