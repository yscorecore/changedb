using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;


namespace ChangeDB.Default
{
    [Service(typeof(IDatabaseMigrate))]
    public class DefaultMigrator : IDatabaseMigrate
    {

        private readonly IAgentFactory _agentFactory;

        public DefaultMigrator(IAgentFactory agentFactory)
        {
            _agentFactory = agentFactory;
        }
        private static Action<string> Log = Console.WriteLine;
        public async Task MigrateDatabase(MigrationContext context)
        {
            var sourceAgent = _agentFactory.CreateAgent(context.SourceDatabase.DatabaseType);
            var targetAgent = _agentFactory.CreateAgent(context.TargetDatabase.DatabaseType);
            await using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);
            await using var targetConnection = targetAgent.CreateConnection(context.TargetDatabase.ConnectionString);

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
                DatabaseType = context.TargetDatabase.DatabaseType,
                Connection = targetConnection,
                Descriptor = sourceDatabaseDescriptor.DeepClone(),
            };
            await ApplyMigrationSettings(source, target, context.Setting);
            await ApplyTargetAgentSettings(target, context.Setting);
            await DoMigrateDatabase(source, target, context.Setting);
            await ApplyCustomScripts(target, context.Setting);

            Log("migration succeeded.");

        }

        private async Task<DatabaseDescriptor> GetSourceDatabaseDescriptor(IMigrationAgent sourceAgent, DbConnection sourceConnection, MigrationSetting migrationSetting)
        {
            Log("start getting source database metadata.");
            return await sourceAgent.MetadataMigrator.GetDatabaseDescriptor(sourceConnection, migrationSetting);
        }

        private Task ApplyMigrationSettings(AgentRunTimeInfo source, AgentRunTimeInfo target, MigrationSetting migrationSetting)
        {
            MigrationSettingsApplier.ApplySettingForTarget(source, target, migrationSetting);
            return Task.CompletedTask;
        }

        private Task ApplyTargetAgentSettings(AgentRunTimeInfo target, MigrationSetting migrationSetting)
        {
            return MigrationSettingsApplier.ApplyAgentSettings(target);
        }

        private async Task DoMigrateDatabase(AgentRunTimeInfo source, AgentRunTimeInfo target, MigrationSetting migrationSetting)
        {
            await CreateTargetDatabase(target.Agent, target.Connection, migrationSetting);

            if (migrationSetting.IncludeMeta)
            {
                await PreMigrationMetadata(target, migrationSetting);
            }

            if (migrationSetting.IncludeData)
            {

                await MigrationData(source, target, migrationSetting);
            }

            if (migrationSetting.IncludeMeta)
            {
                await PostMigrationMetadata(target, migrationSetting);
            }
        }
        private async Task CreateTargetDatabase(IMigrationAgent targetAgent, DbConnection targetConnection, MigrationSetting migrationSetting)
        {
            if (migrationSetting.DropTargetDatabaseIfExists)
            {
                Log("dropping target database if exists.");
                await targetAgent.DatabaseManger.DropDatabaseIfExists(targetConnection, migrationSetting);
            }
            Log("creating target database.");
            await targetAgent.DatabaseManger.CreateDatabase(targetConnection, migrationSetting);
        }
        private async Task PreMigrationMetadata(AgentRunTimeInfo target, MigrationSetting migrationSetting)
        {
            Log("start pre migration metadata.");
            await target.Agent.MetadataMigrator.PreMigrate(target.Descriptor, target.Connection, migrationSetting);
        }
        private async Task PostMigrationMetadata(AgentRunTimeInfo target, MigrationSetting migrationSetting)
        {
            Log("start post migration metadata.");
            await target.Agent.MetadataMigrator.PostMigrate(target.Descriptor, target.Connection, migrationSetting);
        }
        private async Task MigrationData(AgentRunTimeInfo source, AgentRunTimeInfo target, MigrationSetting migrationSetting)
        {
            Log("start migrating data.");
            foreach (var sourceTable in source.Descriptor.Tables)
            {
                await MigrationTable(source, target, migrationSetting, sourceTable);
            }
        }
        private Task ApplyCustomScripts(AgentRunTimeInfo target, MigrationSetting migrationSetting)
        {
            if (migrationSetting.PostScripts?.SqlFiles?.Count > 0)
            {
                Log("apply custom sql scripts");
                target.Connection.ExecuteSqlFiles(migrationSetting.PostScripts.SqlFiles, migrationSetting.PostScripts.SqlSplit);
            }
            return Task.CompletedTask;
        }
        private async Task MigrationTable(AgentRunTimeInfo source, AgentRunTimeInfo target, MigrationSetting migrationSetting, TableDescriptor sourceTable)
        {
            var targetTableName = migrationSetting.TargetNameStyle.TableNameFunc(sourceTable.Name);
            var targetTableDesc = target.Descriptor.GetTable(targetTableName);
            await target.Agent.DataMigrator.BeforeWriteTableData(targetTableDesc, target.Connection, migrationSetting);
            var tableName = string.IsNullOrEmpty(sourceTable.Schema) ? $"\"{sourceTable.Name}\"" : $"\"{sourceTable.Schema}\".\"{sourceTable.Name}\"";
            var totalCount = await source.Agent.DataMigrator.CountTable(sourceTable, source.Connection, migrationSetting);
            var (migratedCount,maxRowSize,fetchCount) = (0,1,1);
           
            while (true)
            {
                var pageInfo = new PageInfo { Offset = migratedCount, Limit = fetchCount };
                var dataTable = await source.Agent.DataMigrator.ReadTableData(sourceTable, pageInfo, source.Connection, migrationSetting);
                // convert target column name
                dataTable.Columns.OfType<DataColumn>().Each(p =>
                    p.ColumnName = migrationSetting.TargetNameStyle.ColumnNameFunc(p.ColumnName));
                await target.Agent.DataMigrator.WriteTableData(dataTable, targetTableDesc, target.Connection, migrationSetting);
                
                migratedCount += dataTable.Rows.Count;
                maxRowSize = Math.Max(maxRowSize, dataTable.MaxRowSize());
                fetchCount = Math.Min(fetchCount*5, Math.Max(1, migrationSetting.FetchDataMaxSize / maxRowSize));
                Log($"migrating table {tableName} ......{migratedCount * 1.0 / totalCount:p2} [{migratedCount}/{totalCount}].");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                if (dataTable.Rows.Count < pageInfo.Limit)
                {
                    Log($"data of table {tableName} migration succeeded, {migratedCount} rows migrated.");
                    break;
                }
            }
            await target.Agent.DataMigrator.AfterWriteTableData(targetTableDesc, target.Connection, migrationSetting);
        }
        
        
        
    }
}
