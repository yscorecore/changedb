using System.Data.Common;

namespace ChangeDB.Migration
{
    public class DatabaseInfo
    {
        public string Type { get; set; }
        public string ConnectionString { get; set; }
    }
}
