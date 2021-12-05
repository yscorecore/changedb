using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;

namespace ChangeDB.Default
{
    [Service(typeof(IDatabaseSqlDumper))]
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

            var sourceDatabaseDescriptor = await GetSourceDatabaseDescriptor(sourceAgent, sourceConnection, context.Setting);

            var source = new AgentRunTimeInfo
            {
                Agent = sourceAgent,
                DatabaseType = context.SourceDatabase.DatabaseType,
                Connection = sourceConnection,
                Descriptor = sourceDatabaseDescriptor,
            };
            var target = new AgentRunTimeInfo
            {
                Agent = targetAgent,
                DatabaseType = context.DumpInfo.DatabaseType,
                // Connection = //targetConnection,
                Descriptor = sourceDatabaseDescriptor.DeepClone(),
            };
            await ApplyMigrationSettings(source, target, context.Setting);
            await ApplyTargetAgentSettings(target, context.Setting);
            await DoMigrateDatabase(source, target, context.Setting);
            await ApplyCustomScripts(target, context.Setting);

            Log("migration succeeded.");

            throw new NotImplementedException();
        }



    }
}
