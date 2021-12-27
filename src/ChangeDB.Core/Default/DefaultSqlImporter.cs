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
        public async Task Import(ImportContext context)
        {
            var targetAgent = AgentFactory.CreateAgent(context.TargetDatabase.DatabaseType);
            await using var targetConnection = targetAgent.CreateConnection(context.TargetDatabase.ConnectionString);

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
                await CreateTargetDatabase(migrationContext);
            }
            targetConnection.ExecuteSqlScriptFile(context.SqlScripts.SqlFile, context.SqlScripts.SqlSplit,
                (info) =>
                {
                    context.ReportSqlExecuted(info.StartLine, info.LineCount, info.Sql, info.Result);
                });
        }
        private async Task CreateTargetDatabase(MigrationContext migrationContext)
        {
            var targetAgent = migrationContext.Target.Agent;
            await targetAgent.DatabaseManger.DropTargetDatabaseIfExists(migrationContext);
            await targetAgent.DatabaseManger.CreateTargetDatabase(migrationContext);
        }
    }
}
