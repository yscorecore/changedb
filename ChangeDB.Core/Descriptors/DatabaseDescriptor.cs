using System.Collections.Immutable;

namespace ChangeDB
{
    public class DatabaseDescriptor
    {
        public ImmutableList<SchemaDescriptor> Schemas { get; set; }
        public ImmutableList<TableDescriptor> Tables { get; set; }
    }
}
