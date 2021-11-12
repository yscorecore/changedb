namespace ChangeDB
{
    public class ColumnDescriptor
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public DBTypeDescriptor DbType { get; set; }
        public string StoreType { get; set; }
        public string DefaultValueSql { get; set; }
        public bool IsNullable { get; set; }
        public bool IsComputed { get; set; }
        public string ComputedColumnSql { get; set; }
        public virtual bool? IsStored { get; set; }
        public virtual ValueGenerated? ValueGenerated { get; set; }
        public string Collation { get; set; }
    }
}
