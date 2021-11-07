using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using YS.Knife;

namespace ChangeDB.Default
{
    [Service]
    public class DefaultMigrator : IDatabaseMigrate
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
            using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);
            using var targetConnection = targetAgent.CreateConnection(context.TargetDatabase.ConnectionString);
            var databaseDescriptor =
                await sourceAgent.MetadataMigrator.GetDatabaseDescriptor(sourceConnection, context.Setting);
            if (context.Setting.IncludeMeta)
            {
                await targetAgent.MetadataMigrator.PreMigrate(databaseDescriptor, targetConnection, context.Setting);

            }

            if (context.Setting.IncludeData)
            {
                await MigrationData(sourceAgent, targetAgent, databaseDescriptor, context, sourceConnection, targetConnection);
            }

            if (context.Setting.IncludeMeta)
            {
                await targetAgent.MetadataMigrator.PostMigrate(databaseDescriptor, targetConnection, context.Setting);

            }
        }



        protected virtual async Task MigrationData(IMigrationAgent source, IMigrationAgent target, DatabaseDescriptor sourceDataBase, MigrationContext context, DbConnection sourceConnection, DbConnection targetConnection)
        {
            foreach (var tableDescriptor in sourceDataBase.Tables)
            {
                var totalCount = await source.DataMigrator.CountTable(tableDescriptor, sourceConnection, context.Setting);
                var migratedCount = 0;
                while (true)
                {
                    var pageInfo = new PageInfo { Offset = migratedCount, Limit = context.Setting.MaxPageSize };
                    var sourceTableData = await source.DataMigrator.ReadTableData(tableDescriptor, pageInfo, sourceConnection, context.Setting);
                    await target.DataMigrator.WriteTableData(sourceTableData, tableDescriptor, targetConnection, context.Setting);
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
