namespace ChangeDB
{
    public class ColumnDescriptor: ExtensionObject
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public string StoreType { get; set; }
        public string DefaultValueSql { get; set; }
        public bool IsNullable { get; set; }
        public string Collation { get; set; }

        #region Computed
        public string ComputedColumnSql { get; set; }
        public virtual bool? IsStored { get; set; }
        #endregion
       // public virtual ValueGenerated? ValueGenerated { get; set; }

        #region Identity
        public bool IsIdentity { get; set; }
        public IdentityDescriptor IdentityInfo { get; set; }
        #endregion
    }
}
