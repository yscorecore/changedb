using System;
using System.Collections.Generic;
using ChangeDB.Migration;

namespace ChangeDB.Default
{
    [Service(typeof(IAgentFactory))]
    public class DefaultAgentFactory : IAgentFactory
    {
        private readonly IDictionary<string, IMigrationAgent> allMigrators;

        public DefaultAgentFactory(IDictionary<string, IMigrationAgent> allMigrators)
        {
            this.allMigrators = allMigrators;
        }
        public IMigrationAgent CreateAgent(string type)
        {
            if (allMigrators.TryGetValue(type, out var migrator))
            {
                return migrator;
            }
            throw new NotSupportedException($"Not support agent type '{type}'.");
        }
    }
}
