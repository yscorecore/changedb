using System;

namespace ChangeDB.Migration
{
    [Obsolete]
    public record AgentRunTimeInfo
    {
        public IAgent Agent { get; set; }
        public DatabaseDescriptor Descriptor { get; set; }
    }
}
