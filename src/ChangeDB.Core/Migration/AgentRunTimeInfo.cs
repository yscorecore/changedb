using System.Data.Common;

namespace ChangeDB.Migration
{
    public record AgentRunTimeInfo
    {
        public string DatabaseType { get; init; }
        public IMigrationAgent Agent { get; init; }
        public DatabaseDescriptor Descriptor { get; init; }
        public DbConnection Connection { get; init; }
    }
}
