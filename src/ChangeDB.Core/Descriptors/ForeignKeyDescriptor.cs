using System.Collections.Generic;

namespace ChangeDB
{
    public class ForeignKeyDescriptor
    {
        public string Schema { get; set; }
        
        public string Name { get; set; }
        
        public string ColumnName { get; set; }
        
        public string ParentSchema { get; set; }
        
        public string ParentTable { get; set; }
        
        public string ParentName { get; set; }
    }
}
