using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ChangeDB.Default;
using ChangeDB.Dump;
using ChangeDB.Import;
using ChangeDB.Migration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ServiceCollection AddChangeDb(this ServiceCollection services)
        {
            services.AddSingleton<IDatabaseMigrate, DefaultMigrator>();
            services.AddSingleton<IAgentFactory, DefaultAgentFactory>();
            services.AddSingleton<IDatabaseSqlDumper, DefaultSqlDumper>();
            services.AddSingleton<IDatabaseSqlImporter, DefaultSqlImporter>();
            AddAgentFactory(services);
            return services;
        }
        private static void AddAgentFactory(ServiceCollection services)
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var agentDlls = Directory.GetFiles(rootPath, "ChangeDB.Agent.*.dll");
            var allAgentTypes = agentDlls.SelectMany(p => Assembly.LoadFrom(p).GetTypes().Where(t => !t.IsAbstract && typeof(IMigrationAgent).IsAssignableFrom(t))).ToList();

            foreach (var agentType in allAgentTypes)
            {
                services.AddSingleton(agentType);
            }

            services.AddSingleton<IDictionary<string, IMigrationAgent>>(sp =>
            {
                var agentInstances = allAgentTypes.Select(p => sp.GetService(p) as IMigrationAgent);
                return agentInstances.ToDictionary(GetAgentName, StringComparer.InvariantCultureIgnoreCase);
            });
        }
        private static string GetAgentName(IMigrationAgent agent)
        {
            var typeName = agent.GetType().Name;
            return typeName.EndsWith("migrationagent", StringComparison.InvariantCultureIgnoreCase) ? typeName[..^"migrationagent".Length] : typeName;
        }
    }
}
