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
    }
}
