using System.Collections.Generic;

namespace ChangeDB
{
    public class UniqueDescriptor : INameObject
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; }
    }
}
