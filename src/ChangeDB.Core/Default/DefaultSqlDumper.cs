using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChangeDB.Dump;
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
            var createNew = context.Setting.DropTargetDatabaseIfExists;
            await using var sqlWriter = new StreamWriter(context.DumpInfo.SqlScriptFile, false);
            await using var targetConnection = new SqlScriptDbConnection(sqlWriter,
                targetAgent.Repr);
            context.SourceConnection = sourceConnection;
            context.TargetConnection = targetConnection;
            context.Source = new AgentRunTimeInfo
            {
                Agent = sourceAgent,
                Descriptor = null,
            };
            context.Writer = sqlWriter;
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
            // flush to file
            await sqlWriter.FlushAsync();
        }
        protected override async Task OnWriteTableData(DataTable dataTable, TableDescriptor tableDescriptor, MigrationContext migrationContext)
        {
            var dataDumper = migrationContext.Target.Agent.DataDumper;
            await dataDumper.WriteTable(dataTable, tableDescriptor, migrationContext as DumpContext);
        }


    }
}
