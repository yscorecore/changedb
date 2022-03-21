﻿using System;
using System.ComponentModel;
using ChangeDB.Descriptors;

namespace ChangeDB
{
    public class ColumnDescriptor : ExtensionObject, INameObject
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        [Obsolete("use DataType")]
        public string StoreType { get; set; }
        public DataTypeDescriptor DataType { get; set; }
        [Obsolete]
        public string DefaultValueSql { get; set; }
        
        public  SqlExpressionDescriptor DefaultValue { get; set; }
        public bool IsNullable { get; set; }
        public string Collation { get; set; }

        #region Computed
        public string ComputedColumnSql { get; set; }
        public bool IsStored { get; set; }
        #endregion

        #region Identity
        public bool IsIdentity { get; set; }
        public IdentityDescriptor IdentityInfo { get; set; }
        #endregion
    }
}
