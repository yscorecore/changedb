namespace ChangeDB.Migration
{
    public interface IAgentFactory
    {
        IMigrationAgent CreateAgent(string type);
    }
}
