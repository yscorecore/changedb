using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;


namespace ChangeDB.Default
{
    [Service(typeof(IDatabaseMigrate))]
    public class DefaultMigrator : IDatabaseMigrate
    {

        protected IAgentFactory AgentFactory { get; }

        public DefaultMigrator(IAgentFactory agentFactory)
        {
            AgentFactory = agentFactory;
        }
        protected static Action<string> Log = Console.WriteLine;
        public virtual async Task MigrateDatabase(MigrationContext context)
        {
            var sourceAgent = AgentFactory.CreateAgent(context.SourceDatabase.DatabaseType);
            var targetAgent = AgentFactory.CreateAgent(context.TargetDatabase.DatabaseType);
            await using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);
            await using var targetConnection = targetAgent.CreateConnection(context.TargetDatabase.ConnectionString);

            var sourceDatabaseDescriptor = await GetSourceDatabaseDescriptor(sourceAgent, sourceConnection, context);
            context.Source = new AgentRunTimeInfo
            {
                Agent = sourceAgent,
                DatabaseType = context.SourceDatabase.DatabaseType,
                Connection = sourceConnection,
                Descriptor = sourceDatabaseDescriptor,
            };
            context.Target = new AgentRunTimeInfo
            {
                Agent = targetAgent,
                DatabaseType = context.TargetDatabase.DatabaseType,
                Connection = targetConnection,
                Descriptor = sourceDatabaseDescriptor.DeepClone(),
            };


            await ApplyMigrationSettings(context);
            await ApplyTargetAgentSettings(context);
            await DoMigrateDatabase(context);
            await ApplyCustomScripts(context);

            Log("migration succeeded.");

        }

        protected virtual async Task<DatabaseDescriptor> GetSourceDatabaseDescriptor(IMigrationAgent sourceAgent, DbConnection sourceConnection, MigrationContext migrationContext)
        {
            Log("start getting source database metadata.");
            return await sourceAgent.MetadataMigrator.GetSourceDatabaseDescriptor(migrationContext);
        }

        protected virtual Task ApplyMigrationSettings(MigrationContext migrationContext)
        {
            SettingsApplier.ApplySettingForTarget(migrationContext.Source, migrationContext.Target, migrationContext.Setting);
            return Task.CompletedTask;
        }

        protected virtual Task ApplyTargetAgentSettings(MigrationContext migrationContext)
        {
            return SettingsApplier.ApplyAgentSettings(migrationContext.Target);
        }

        protected virtual async Task DoMigrateDatabase(MigrationContext migrationContext)
        {
            var (target, source, migrationSetting) = (migrationContext.Target, migrationContext.Source, migrationContext.Setting);
            await CreateTargetDatabase(migrationContext);

            if (migrationContext.Setting.IncludeMeta)
            {
                await PreMigrationMetadata(target, migrationContext);
            }

            if (migrationContext.Setting.IncludeData)
            {

                await MigrationData(source, target, migrationContext);
            }

            if (migrationContext.Setting.IncludeMeta)
            {
                await PostMigrationMetadata(target, migrationContext);
            }
        }
        protected virtual async Task CreateTargetDatabase(MigrationContext migrationContext)
        {
            var (targetAgent, targetConnection) = (migrationContext.Target.Agent, migrationContext.Target.Connection);
            if (migrationContext.Setting.DropTargetDatabaseIfExists)
            {
                Log("dropping target database if exists.");
                await targetAgent.DatabaseManger.DropTargetDatabaseIfExists(migrationContext);
            }
            Log("creating target database.");
            await targetAgent.DatabaseManger.CreateTargetDatabase(migrationContext);
        }
        protected virtual async Task PreMigrationMetadata(AgentRunTimeInfo target, MigrationContext migrationContext)
        {
            Log("start pre migration metadata.");
            await target.Agent.MetadataMigrator.PreMigrateTargetMetadata(target.Descriptor, migrationContext);
        }
        protected virtual async Task PostMigrationMetadata(AgentRunTimeInfo target, MigrationContext migrationContext)
        {
            Log("start post migration metadata.");
            await target.Agent.MetadataMigrator.PostMigrateTargetMetadata(target.Descriptor, migrationContext);
        }
        protected virtual async Task MigrationData(AgentRunTimeInfo source, AgentRunTimeInfo target, MigrationContext migrationContext)
        {
            Log("start migrating data.");
            foreach (var sourceTable in source.Descriptor.Tables)
            {
                await MigrationTable(source, target, migrationContext, sourceTable);
            }
        }
        protected virtual Task ApplyCustomScripts(MigrationContext migrationContext)
        {
            var migrationSetting = migrationContext.Setting;
            if (migrationSetting.PostScripts?.SqlFiles?.Count > 0)
            {
                Log("apply custom sql scripts");
                migrationContext.Target.Connection.ExecuteSqlFiles(migrationSetting.PostScripts.SqlFiles, migrationSetting.PostScripts.SqlSplit);
            }
            return Task.CompletedTask;
        }
        protected virtual async Task MigrationTable(AgentRunTimeInfo source, AgentRunTimeInfo target, MigrationContext migrationContext, TableDescriptor sourceTable)
        {
            var migrationSetting = migrationContext.Setting;
            var targetTableName = migrationSetting.TargetNameStyle.TableNameFunc(sourceTable.Name);
            var targetTableDesc = target.Descriptor.GetTable(targetTableName);
            await target.Agent.DataMigrator.BeforeWriteTargetTable(targetTableDesc, migrationContext);
            var tableName = string.IsNullOrEmpty(sourceTable.Schema) ? $"\"{sourceTable.Name}\"" : $"\"{sourceTable.Schema}\".\"{sourceTable.Name}\"";
            var totalCount = await source.Agent.DataMigrator.CountSourceTable(sourceTable, migrationContext);
            var (migratedCount, maxRowSize, fetchCount) = (0, 1, 1);

            while (true)
            {
                var pageInfo = new PageInfo { Offset = migratedCount, Limit = Math.Max(1, fetchCount) };
                var dataTable = await source.Agent.DataMigrator.ReadSourceTable(sourceTable, pageInfo, migrationContext);
                // convert target column name
                dataTable.Columns.OfType<DataColumn>().Each(p =>
                    p.ColumnName = migrationSetting.TargetNameStyle.ColumnNameFunc(p.ColumnName));
                await target.Agent.DataMigrator.WriteTargetTable(dataTable, targetTableDesc, migrationContext);

                migratedCount += dataTable.Rows.Count;
                maxRowSize = Math.Max(maxRowSize, dataTable.MaxRowSize());
                fetchCount = Math.Min(fetchCount * migrationSetting.GrowthSpeed, Math.Max(1, migrationSetting.FetchDataMaxSize / maxRowSize));
                Log($"migrating table {tableName} ......{migratedCount * 1.0 / totalCount:p2} [{migratedCount}/{totalCount}].");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                if (dataTable.Rows.Count < pageInfo.Limit)
                {
                    Log($"data of table {tableName} migration succeeded, {migratedCount} rows migrated.");
                    break;
                }
            }
            await target.Agent.DataMigrator.AfterWriteTargetTable(targetTableDesc, migrationContext);
        }



    }
}
