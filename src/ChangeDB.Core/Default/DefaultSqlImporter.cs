using System.Threading.Tasks;
using ChangeDB.Import;
using ChangeDB.Migration;

namespace ChangeDB.Default
{
    public class DefaultSqlImporter : IDatabaseSqlImporter
    {
        protected IAgentFactory AgentFactory { get; }

        public DefaultSqlImporter(IAgentFactory agentFactory)
        {
            AgentFactory = agentFactory;
        }

        public async Task Import(ImportSetting setting,IEventReporter eventReporter)
        {
            var targetAgent = AgentFactory.CreateAgent(setting.TargetDatabase.DatabaseType);
            await using var targetConnection = targetAgent.ConnectionProvider.CreateConnection(setting.TargetDatabase.ConnectionString);

            var context = new ImportContext()
            {
                Connection = targetConnection,
                ConnectionString = setting.TargetDatabase.ConnectionString,
                Agent = targetAgent,
                Setting = setting,
                EventReporter = eventReporter
            };
            var sqlExecutor = targetAgent.SqlExecutor;
            if (setting.ReCreateTargetDatabase)
            {
                await CreateTargetDatabase(targetAgent, setting.TargetDatabase.ConnectionString);
            }
            await sqlExecutor.ExecuteFile(setting.SqlScripts.SqlFile, context);
        }

        private async Task CreateTargetDatabase(IAgent agent, string connectionString)
        {
            await agent.DatabaseManger.DropDatabaseIfExists(connectionString);
            await agent.DatabaseManger.CreateDatabase(connectionString);
        }
    }
}
