using System.Collections.Generic;

namespace ChangeDB
{
    public class ForeignKeyDescriptor
    {
        public string Schema { get; set; }

        public string Name { get; set; }

        public List<string> ColumnNames { get; set; }

        public string ParentSchema { get; set; }

        public string ParentTable { get; set; }

        public List<string> ParentNames { get; set; }

        public ReferentialAction? OnDelete { get; set; }
    }
}
