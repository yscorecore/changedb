using System.Collections.Generic;

namespace ChangeDB
{
    public record ExtensionObject
    {
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
    }
}
