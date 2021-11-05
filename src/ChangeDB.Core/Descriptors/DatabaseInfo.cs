using System.Data.Common;

namespace ChangeDB
{
    public class DatabaseInfo : System.IDisposable
    {
        public string Type { get; set; }
        public DbConnection Connection { get; set; }
        public void Dispose()
        {
            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
        }
    }
}
