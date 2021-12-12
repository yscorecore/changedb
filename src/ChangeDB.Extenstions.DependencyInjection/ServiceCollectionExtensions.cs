using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Default;
using ChangeDB.Dump;
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
            AddAgentFactorys(services);
            return services;
        }
        private static void AddAgentFactorys(ServiceCollection services)
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var agentDlls = Directory.GetFiles(rootPath, "ChangeDB.Agent.*.dll");
            var allAgentTypes = agentDlls.SelectMany(p => Assembly.LoadFrom(p).GetTypes().Where(p => !p.IsAbstract && typeof(IMigrationAgent).IsAssignableFrom(p))).ToList();

            foreach (var agentType in allAgentTypes)
            {
                services.AddSingleton(agentType);
            }

            services.AddSingleton<IDictionary<string, IMigrationAgent>>(sp =>
            {
                var agentInstances = allAgentTypes.Select(p => sp.GetService(p) as IMigrationAgent);
                return agentInstances.ToDictionary(p => GetAgentName(p), StringComparer.InvariantCultureIgnoreCase);
            });
        }
        private static string GetAgentName(object agent)
        {
            var typeName = agent.GetType().Name.ToLowerInvariant();
            if (typeName.EndsWith("migrationagent"))
            {
                return typeName.Substring(0, typeName.Length - "migrationagent".Length);
            }
            return typeName;

        }
    }
}
