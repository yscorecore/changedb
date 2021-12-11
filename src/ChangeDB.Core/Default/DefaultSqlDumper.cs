//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.Common;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using ChangeDB.Dump;
//using ChangeDB.Migration;

//namespace ChangeDB.Default
//{
//    [Service(typeof(IDatabaseSqlDumper))]
//    public class DefaultSqlDumper : DefaultMigrator, IDatabaseSqlDumper
//    {


//        public DefaultSqlDumper(IAgentFactory agentFactory) : base(agentFactory)
//        {
//        }
//        public async Task DumpSql(DumpContext context)
//        {
//            var sourceAgent = AgentFactory.CreateAgent(context.SourceDatabase.DatabaseType);
//            var targetAgent = AgentFactory.CreateAgent(context.DumpInfo.DatabaseType);
//            await using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);

//            var sourceDatabaseDescriptor = await GetSourceDatabaseDescriptor(sourceAgent, sourceConnection, context);

//            var source = new AgentRunTimeInfo
//            {
//                Agent = sourceAgent,
//                DatabaseType = context.SourceDatabase.DatabaseType,
//                Connection = sourceConnection,
//                Descriptor = sourceDatabaseDescriptor,
//            };
//            var target = new AgentRunTimeInfo
//            {
//                Agent = targetAgent,
//                DatabaseType = context.DumpInfo.DatabaseType,
//                Connection = new Fakes.SqlScriptDbConnection(context.DumpInfo.SqlScriptFile, (val) => targetAgent.Repr.ReprValue(val)),
//                Descriptor = sourceDatabaseDescriptor.DeepClone(),
//            };
//            await ApplyMigrationSettings(source, target, context);
//            await ApplyTargetAgentSettings(target, context);
//            await DoMigrateDatabase(source, target, context);
//            await ApplyCustomScripts(target, context);

//            Log($"dump to file '{context.DumpInfo.SqlScriptFile}' succeeded.");

//        }



//        protected override async Task DoMigrateDatabase(AgentRunTimeInfo source, AgentRunTimeInfo target, MigrationContext migrationContext)
//        {
//            var migrationSetting = migrationContext.Setting;

//            if (migrationSetting.IncludeMeta)
//            {
//                await PreMigrationMetadata(target, migrationContext);
//            }

//            if (migrationSetting.IncludeData)
//            {

//                await MigrationData(source, target, migrationContext);
//            }

//            if (migrationSetting.IncludeMeta)
//            {
//                await PostMigrationMetadata(target, migrationContext);
//            }
//        }
//    }
//}
