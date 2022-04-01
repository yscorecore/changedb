namespace ChangeDB
{
    public interface IAgentFactory
    {
        IAgent CreateAgent(string type);
    }
}
