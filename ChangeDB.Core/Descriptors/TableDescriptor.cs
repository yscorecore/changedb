using System.Collections.Generic;

namespace ChangeDB
{
    public class TableDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ColumnDescriptor> Columns { get; set; }
    }
}
