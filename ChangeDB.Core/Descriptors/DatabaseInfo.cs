using System.Data.Common;

namespace ChangeDB
{
    public class DatabaseInfo
    {
        public string Type { get; set; }
        public DbConnection Connection { get; set; }
    }
}
