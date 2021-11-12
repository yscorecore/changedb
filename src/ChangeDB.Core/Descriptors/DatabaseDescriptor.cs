using System.Collections.Generic;
using System.Collections.Immutable;

namespace ChangeDB
{
    public class DatabaseDescriptor
    {
        public string Collation { get; set; }
        public string DefaultSchema { get; set; }
        public List<string> Schemas { get; set; }
        public List<TableDescriptor> Tables { get; set; }
    }
}
