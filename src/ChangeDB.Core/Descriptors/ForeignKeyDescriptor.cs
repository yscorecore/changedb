using System.Collections.Generic;

namespace ChangeDB
{
    public class ForeignKeyDescriptor
    {
        public List<string> ColumnNames { get; set; }
        
        public string ParentSchema { get; set; }
        
        public string ParentTable { get; set; }
        
        public List<string> ParentNames { get; set; }
    }
}
