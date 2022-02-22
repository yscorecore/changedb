using System;
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

        protected override async Task MigrationTable(MigrationContext migrationContext, TableDescriptor sourceTable)
        {
            var dumpContext = (DumpContext)migrationContext;
            var (source, target) = (migrationContext.Source, migrationContext.Target);
            var migrationSetting = migrationContext.Setting;
            var targetTableName = migrationSetting.TargetNameStyle.TableNameFunc(sourceTable.Name);
            var targetTableDesc = target.Descriptor.GetTable(targetTableName);
            var targetTableFullName = migrationContext.Target.Agent.AgentSetting.IdentityName(targetTableDesc.Schema, targetTableDesc.Name);

            await target.Agent.DataMigrator.BeforeWriteTargetTable(targetTableDesc, migrationContext);
            var totalCount = await source.Agent.DataMigrator.CountSourceTable(sourceTable, migrationContext);
            var (migratedCount, maxRowSize, fetchCount) = (0, 1, 1);
            await target.Agent.DataDumper.BeforeWriteTable(targetTableDesc, dumpContext);

            while (totalCount > 0)
            {


                var pageInfo = new PageInfo { Offset = migratedCount, Limit = Math.Max(1, fetchCount) };
                var dataTable = await source.Agent.DataMigrator.ReadSourceTable(sourceTable, pageInfo, migrationContext);
                // convert target column name
                dataTable.Columns.OfType<DataColumn>().Each(p =>
                    p.ColumnName = migrationSetting.TargetNameStyle.ColumnNameFunc(p.ColumnName));
                await target.Agent.DataDumper.WriteTable(dataTable, targetTableDesc, dumpContext);

                migratedCount += dataTable.Rows.Count;
                totalCount = Math.Max(totalCount, migratedCount);
                maxRowSize = Math.Max(maxRowSize, dataTable.MaxRowSize());
                fetchCount = Math.Min(fetchCount * migrationSetting.GrowthSpeed, Math.Max(1, migrationSetting.FetchDataMaxSize / maxRowSize));

                if (dataTable.Rows.Count < pageInfo.Limit)
                {
                    migrationContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, migratedCount, migratedCount, false);
                    break;
                }
                migrationContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, totalCount, migratedCount, false);
            }
            await target.Agent.DataDumper.AfterWriteTable(targetTableDesc, dumpContext);
            await target.Agent.DataMigrator.AfterWriteTargetTable(targetTableDesc, migrationContext);

            migrationContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, migratedCount, migratedCount, true);
        }

    }
}
