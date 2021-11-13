using System.Collections.Generic;

namespace ChangeDB
{
    public class PrimaryKeyDescriptor
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; }
    }
}
