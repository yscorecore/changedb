namespace ChangeDB.Migration
{
    public record AgentRunTimeInfo
    {
        public IMigrationAgent Agent { get; set; }
        public DatabaseDescriptor Descriptor { get; set; }
    }
}
