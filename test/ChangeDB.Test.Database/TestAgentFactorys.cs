using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ChangeDB
{
    public class TestAgentFactorys
    {
        private readonly static IDictionary<string, IAgent> AllAgents;
        static TestAgentFactorys()
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var agentDlls = Directory.GetFiles(rootPath, "ChangeDB.Agent.*.dll");
            var allAgentTypes = agentDlls
                .SelectMany(p => Assembly.LoadFrom(p).GetTypes().Where(t => !t.IsAbstract && typeof(IAgent).IsAssignableFrom(t)))
                .ToList();
            AllAgents = allAgentTypes.Select(p => Activator.CreateInstance(p) as IAgent)
                 .ToDictionary(GetAgentName, StringComparer.InvariantCultureIgnoreCase);
        }
        private static string GetAgentName(IAgent agent)
        {
            var typeName = agent.GetType().Name;
            return typeName.EndsWith("agent", StringComparison.InvariantCultureIgnoreCase) ? typeName[..^"agent".Length] : typeName;
        }
        public static IEnumerable<string> GetAllAgents()
        {
            return AllAgents.Keys;
        }

        public static IAgent GetAgentByDbType(string dbType)
        {
            if (AllAgents.TryGetValue(dbType, out IAgent agent))
            {
                return agent;
            }
            else
            {
                throw new Exception($"Not support agent type '{dbType}'");
            }
        }
    }
}
