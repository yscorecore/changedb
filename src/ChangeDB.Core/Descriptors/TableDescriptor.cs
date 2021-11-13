using System.Collections.Generic;
using System.Collections.Immutable;

namespace ChangeDB
{
    public class TableDescriptor
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Schema { get; set; }

        public List<ColumnDescriptor> Columns { get; set; } = new List<ColumnDescriptor>();

        public PrimaryKeyDescriptor PrimaryKey { get; set; }

        public List<IndexDescriptor> Indexes { get; set; } = new List<IndexDescriptor>();

        public List<ForeignKeyDescriptor> ForeignKeys { get; set; } = new List<ForeignKeyDescriptor>();

        public List<UniqueDescriptor> Uniques { get; set; } = new List<UniqueDescriptor>();

    }
}
