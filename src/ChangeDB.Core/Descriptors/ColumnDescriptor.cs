namespace ChangeDB
{
    public class ColumnDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DBTypeDescriptor DbType { get; set; }
        public string DefaultValue { get; set; }
        public bool AllowNull { get; set; }
        //public bool IsPrimaryKey { get; set; }
        public bool IsComputed { get; set; }
    }
}
