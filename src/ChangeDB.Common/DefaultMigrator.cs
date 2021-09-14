using System;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Common
{
    public class DefaultMigrator:IDatabaseMigrate
    {
        private readonly IAgentFactory _agentFactory;

        public DefaultMigrator(IAgentFactory agentFactory)
        {
            _agentFactory = agentFactory;
        }
        public async Task MigrateDatabase(MigrationContext context)
        {
            var sourceAgent = _agentFactory.CreateAgent(context.SourceDatabase.Type);
            var targetAgent = _agentFactory.CreateAgent(context.TargetDatabase.Type);
            var databaseDescriptor =
                await sourceAgent.MetadataMigrator.GetDatabaseDescriptor(context.SourceDatabase, context.Setting);
            if (context.Setting.IncludeMeta)
            {
                await PreMigrate(context, targetAgent, databaseDescriptor);
            }

            if (context.Setting.IncludeData)
            {
                await MigrationData(sourceAgent, targetAgent, databaseDescriptor, context);
            }

            if (context.Setting.IncludeMeta)
            {
                await PostMigrate(context, targetAgent, databaseDescriptor);
            }
        }

        protected  virtual Task PostMigrate(MigrationContext context, IMigrationAgent targetAgent, DatabaseDescriptor databaseDescriptor)
        {
            return targetAgent.MetadataMigrator.PostMigrate(databaseDescriptor, context.Setting);
        }

        protected  virtual async Task PreMigrate(MigrationContext context, IMigrationAgent targetAgent,
            DatabaseDescriptor databaseDescriptor)
        {
            await targetAgent.MetadataMigrator.PreMigrate(databaseDescriptor, context.Setting);
        }

        protected virtual async Task MigrationData(IMigrationAgent source, IMigrationAgent target, DatabaseDescriptor sourceDataBase, MigrationContext context)
        {
            foreach (var tableDescriptor in sourceDataBase.Tables)
            {
                var totalCount = await source.DataMigrator.CountTable(tableDescriptor, context.Setting);
                var migratedCount = 0;
                while (true)
                {
                    var pageInfo = new PageInfo {Offset = migratedCount, Limit = context.Setting.MaxPageSize};
                    var sourceTableData = await source.DataMigrator.ReadTableData(tableDescriptor, pageInfo, context.Setting);
                    await target.DataMigrator.WriteTableData(sourceTableData, tableDescriptor, context.Setting);
                    if (sourceTableData.Rows.Count < context.Setting.MaxPageSize)
                    {
                        // end of table
                        break;
                    }
                    else
                    {
                        migratedCount += sourceTableData.Rows.Count;
                    }
                }
            }
        }
        
    }
}
