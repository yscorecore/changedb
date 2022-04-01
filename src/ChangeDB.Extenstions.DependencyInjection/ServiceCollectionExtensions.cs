using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ChangeDB;
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
            services.AddSingleton<IDatabaseMapper, DefaultDatabaseMapper>();
            services.AddSingleton<ITableDataMapper, DefaultTableDataMapper>();
            AddAgentFactory(services);
            return services;
        }
        private static void AddAgentFactory(ServiceCollection services)
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var agentDlls = Directory.GetFiles(rootPath, "ChangeDB.Agent.*.dll");
            var allAgentTypes = agentDlls.SelectMany(p => Assembly.LoadFrom(p).GetTypes().Where(t => !t.IsAbstract && typeof(IAgent).IsAssignableFrom(t))).ToList();

            foreach (var agentType in allAgentTypes)
            {
                services.AddSingleton(agentType);
            }

            services.AddSingleton<IDictionary<string, IAgent>>(sp =>
            {
                var agentInstances = allAgentTypes.Select(p => sp.GetService(p) as IAgent);
                return agentInstances.ToDictionary(GetAgentName, StringComparer.InvariantCultureIgnoreCase);
            });
        }
        private static string GetAgentName(IAgent agent)
        {
            var typeName = agent.GetType().Name;
            return typeName.EndsWith("migrationagent", StringComparison.InvariantCultureIgnoreCase) ? typeName[..^"migrationagent".Length] : typeName;
        }
    }
}
