using System;
using System.ComponentModel;
using ChangeDB.Descriptors;

namespace ChangeDB
{
    public class ColumnDescriptor : ExtensionObject, INameObject
    {

        public string Name { get; set; }
        public string Comment { get; set; }

        public DataTypeDescriptor DataType { get; set; }


        public SqlExpressionDescriptor DefaultValue { get; set; }
        public bool IsNullable { get; set; }
        public string Collation { get; set; }

        #region Computed
        [Obsolete]
        public string ComputedColumnSql { get; set; }
        public bool IsStored { get; set; }
        #endregion

        #region Identity
        public bool IsIdentity { get; set; }
        public IdentityDescriptor IdentityInfo { get; set; }
        #endregion
    }
    public static class ColumnDescriptorExtensions
    {
        public const string OriginStoreTypeKey = "changedb::OriginStoreType";
        public const string OriginDefaultValueKey = "changedb::OriginDefaultValue";
        public static string GetOriginStoreType(this ColumnDescriptor columnDescriptor)
        {
            if (columnDescriptor.Values.TryGetValue(OriginStoreTypeKey, out var storeType))
            {
                return storeType as string;
            }
            return default;
        }
        public static void SetOriginStoreType(this ColumnDescriptor columnDescriptor, string storeType)
        {
            if (string.IsNullOrEmpty(storeType))
            {
                columnDescriptor.Values.Remove(OriginStoreTypeKey);
            }
            else
            {
                columnDescriptor.Values[OriginStoreTypeKey] = storeType;
            }


        }

        public static string GetOriginDefaultValue(this ColumnDescriptor columnDescriptor)
        {
            if (columnDescriptor.Values.TryGetValue(OriginDefaultValueKey, out var defaultValue))
            {
                return defaultValue as string;
            }
            return default;
        }
        public static void SetOriginDefaultValue(this ColumnDescriptor columnDescriptor, string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue))
            {
                columnDescriptor.Values.Remove(OriginDefaultValueKey);
            }
            else
            {
                columnDescriptor.Values[OriginDefaultValueKey] = defaultValue;
            }
        }
    }
}
