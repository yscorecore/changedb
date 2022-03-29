using System.Collections.Generic;

namespace ChangeDB
{
    public class IndexDescriptor : INameObject
    {
        public string Name { get; set; }
        public bool IsUnique { get; set; }
        public string Filter { get; set; }
        public List<string> Columns { get; set; }
    }
}
