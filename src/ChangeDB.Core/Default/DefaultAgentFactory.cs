using System;
using System.Collections.Generic;
using ChangeDB.Migration;

namespace ChangeDB.Default
{
    public class DefaultAgentFactory : IAgentFactory
    {
        private readonly IDictionary<string, IMigrationAgent> _allMigrators;

        public DefaultAgentFactory(IDictionary<string, IMigrationAgent> allMigrators)
        {
            this._allMigrators = allMigrators;
        }
        public IMigrationAgent CreateAgent(string type)
        {
            if (_allMigrators.TryGetValue(type, out var migrator))
            {
                return migrator;
            }
            throw new NotSupportedException($"Not support agent type '{type}', the agent type should be one of the [{string.Join(", ", _allMigrators.Keys)}].");
        }
    }
}
