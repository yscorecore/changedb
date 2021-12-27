using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Fakes;
using ChangeDB.Migration;

namespace ChangeDB.Default
{
    public class DefaultSqlDumper : DefaultMigrator, IDatabaseSqlDumper
    {
        public DefaultSqlDumper(IAgentFactory agentFactory) : base(agentFactory)
        {
        }
        public async Task DumpSql(DumpContext context)
        {

            var sourceAgent = AgentFactory.CreateAgent(context.SourceDatabase.DatabaseType);
            var targetAgent = AgentFactory.CreateAgent(context.DumpInfo.DatabaseType);
            await using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);

            context.SourceConnection = sourceConnection;
            context.TargetConnection = new SqlScriptDbConnection(context.DumpInfo.SqlScriptFile, targetAgent.Repr.ReprValue);
            context.Source = new AgentRunTimeInfo
            {
                Agent = sourceAgent,
                Descriptor = null,
            };
            var sourceDatabaseDescriptor = await GetSourceDatabaseDescriptor(sourceAgent, sourceConnection, context);

            context.Source.Descriptor = sourceDatabaseDescriptor;

            context.Target = new AgentRunTimeInfo
            {
                Agent = targetAgent,
                Descriptor = sourceDatabaseDescriptor.DeepClone(),
            };


            await ApplyMigrationSettings(context);
            await ApplyTargetAgentSettings(context);
            await DoMigrateDatabase(context);
            await ApplyCustomScripts(context);

        }


    }
}
