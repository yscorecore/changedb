using System;

namespace ChangeDB
{
    public record DatabaseInfo
    {
        public string DatabaseType { get; set; }
        public string ConnectionString { get; set; }
    }
}
