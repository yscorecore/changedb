using System.Collections.Generic;

namespace ChangeDB
{
    public record PrimaryKeyDescriptor : INameObject
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; }
    }
}
