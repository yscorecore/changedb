using System.Collections.Generic;
using System.Collections.Immutable;

namespace ChangeDB
{
    public class TableDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Schema { get; set; }
        public List<ColumnDescriptor> Columns { get; set; }
        
        public PrimaryKeyDescriptor PrimaryKey { get; set; }
        
        public List<IndexDescriptor> Indexes { get; set; }
        
        public List<ForeignKeyDescriptor> ForeignKeys { get; set; }
        
        public List<UniqueDescriptor> Uniques { get; set; }
        
    }
}
