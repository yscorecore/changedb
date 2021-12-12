using System.Data.Common;

namespace ChangeDB.Migration
{
    public record AgentRunTimeInfo
    {
        public string DatabaseType { get; set; }
        public IMigrationAgent Agent { get; set; }
        public DatabaseDescriptor Descriptor { get; set; }
        public DbConnection Connection { get; set; }
    }
}
