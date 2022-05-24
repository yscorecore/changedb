using System.Collections.Generic;

namespace ChangeDB
{
    public interface IAgentFactory
    {
        IAgent CreateAgent(string type);
        IEnumerable<IAgent> ListAll();
    }
}
