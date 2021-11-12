using System.Collections.Generic;

namespace ChangeDB
{
    public class UniqueDescriptor
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public List<string> Columns { get; set; }
    }
}
