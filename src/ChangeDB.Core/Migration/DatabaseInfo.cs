using System.Data.Common;

namespace ChangeDB.Migration
{
    public class DatabaseInfo
    {
        public string DatabaseType { get; set; }
        public string ConnectionString { get; set; }
    }
}
