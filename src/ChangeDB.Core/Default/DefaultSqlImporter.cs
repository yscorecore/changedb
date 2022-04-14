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

        [System.Obsolete]
        public async Task Import(ImportContext context)
        {
            var targetAgent = AgentFactory.CreateAgent(context.TargetDatabase.DatabaseType);
            await using var targetConnection = targetAgent.ConnectionProvider.CreateConnection(context.TargetDatabase.ConnectionString);

            var migrationContext = new MigrationContext()
            {
                TargetConnection = targetConnection,
                Target = new AgentRunTimeInfo()
                {
                    Agent = targetAgent,
                }
            };
            migrationContext.EventReporter.ObjectCreated += (s, e) =>
            {
                context.ReportObjectCreated(e);
            };
            if (context.ReCreateTargetDatabase)
            {
                await CreateTargetDatabase(targetAgent, context.TargetDatabase.ConnectionString);
            }
            targetConnection.ExecuteSqlScriptFile(context.SqlScripts.SqlFile, context.SqlScripts.SqlSplit,
                (info) =>
                {
                    context.ReportSqlExecuted(info.StartLine, info.LineCount, info.Sql, info.Result);
                });
        }

        [System.Obsolete]
        private async Task CreateTargetDatabase(IAgent agent, string connectionString)
        {

            await agent.DatabaseManger.DropTargetDatabaseIfExists(connectionString, new MigrationSetting());
            await agent.DatabaseManger.CreateDatabase(connectionString, new MigrationSetting());
        }
    }
}
