using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using ChangeDB.Migration.Mapper;


namespace ChangeDB.Default
{
    public class DefaultMigrator : IDatabaseMigrate
    {
        protected IAgentFactory AgentFactory { get; }


        private IDatabaseMapper _databaseMapper;

        private ITableDataMapper _tableDataMapper;

        public DefaultMigrator(IAgentFactory agentFactory, IDatabaseMapper databaseMapper, ITableDataMapper tableDataMapper)
        {
            AgentFactory = agentFactory;
            _databaseMapper = databaseMapper;
            _tableDataMapper = tableDataMapper;
        }

        protected static Action<string> Log = (a) => { };
        public virtual async Task MigrateDatabase(MigrationContext context)
        {
            var sourceAgent = AgentFactory.CreateAgent(context.SourceDatabase.DatabaseType);
            var targetAgent = AgentFactory.CreateAgent(context.TargetDatabase.DatabaseType);
            await using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);
            await using var targetConnection = targetAgent.CreateConnection(context.TargetDatabase.ConnectionString);

            context.SourceConnection = sourceConnection;
            context.TargetConnection = targetConnection;
            context.Source = new AgentRunTimeInfo
            {
                Agent = sourceAgent,
                Descriptor = null,
            };
            context.Target = new AgentRunTimeInfo
            {
                Agent = targetAgent,
                Descriptor = null,
            };
            var sourceDatabaseDescriptor = await sourceAgent.MetadataMigrator.GetSourceDatabaseDescriptor(context);

            var databaseDescriptorMapper =
                await _databaseMapper.MapDatabase(sourceDatabaseDescriptor, targetAgent.AgentSetting, context.Setting);

            context.DatabaseMapper = databaseDescriptorMapper;
            context.Source.Descriptor = databaseDescriptorMapper.Source;
            context.Target.Descriptor = databaseDescriptorMapper.Target;

            await DoMigrateDatabase(context);
            await ApplyCustomScripts(context);

        }




        protected virtual async Task DoMigrateDatabase(MigrationContext migrationContext)
        {
            var (target, source, migrationSetting) = (migrationContext.Target, migrationContext.Source, migrationContext.Setting);
            if (migrationContext.Setting.IncludeMeta)
            {
                await CreateTargetDatabase(migrationContext);
            }

            if (migrationContext.Setting.IncludeMeta)
            {
                await PreMigrationMetadata(target, migrationContext);
            }

            if (migrationContext.Setting.IncludeData)
            {
                await MigrationData(migrationContext);
            }

            if (migrationContext.Setting.IncludeMeta)
            {
                await PostMigrationMetadata(target, migrationContext);
            }
        }
        protected virtual async Task CreateTargetDatabase(MigrationContext migrationContext)
        {
            var (targetAgent, targetConnection) = (migrationContext.Target.Agent, migrationContext.TargetConnection);
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
            migrationContext.RaiseStageChanged(StageKind.StartingPreMeta);
            await target.Agent.MetadataMigrator.PreMigrateTargetMetadata(target.Descriptor, migrationContext);
            migrationContext.RaiseStageChanged(StageKind.FinishedPreMeta);
        }
        protected virtual async Task PostMigrationMetadata(AgentRunTimeInfo target, MigrationContext migrationContext)
        {
            migrationContext.RaiseStageChanged(StageKind.StartingPostMeta);
            await target.Agent.MetadataMigrator.PostMigrateTargetMetadata(target.Descriptor, migrationContext);
            migrationContext.RaiseStageChanged(StageKind.FinishedPostMeta);
        }
        protected virtual async Task MigrationData(MigrationContext migrationContext)
        {
            migrationContext.EventReporter.RaiseStageChanged(StageKind.StartingTableData);
            if (NeedOrderByDependency())
            {
                await MigrateTableInSingleTask(OrderByDependency(migrationContext.DatabaseMapper.TableMappers));
            }
            else
            {
                if (NeedMultiTask())
                {
                    await MigrateTableInParallelTasks();
                }
                else
                {
                    await MigrateTableInSingleTask(migrationContext.DatabaseMapper.TableMappers);
                }
            }
            migrationContext.EventReporter.RaiseStageChanged(StageKind.FinishedTableData);

            bool NeedOrderByDependency() => migrationContext.Setting.MigrationScope == MigrationScope.Data;

            bool NeedMultiTask() => migrationContext.Setting.MaxTaskCount > 1;

            async Task MigrateTableInSingleTask(IEnumerable<TableDescriptorMapper> tableMappers)
            {
                // single task
                foreach (var tableMapper in tableMappers)
                {
                    await MigrationTable(migrationContext, tableMapper);
                }
            }
            Task MigrateTableInParallelTasks()
            {
                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = migrationContext.Setting.MaxTaskCount
                };
                _ = Parallel.ForEach(migrationContext.DatabaseMapper.TableMappers, options, async (table) =>
                {
                    using var fordedContext = migrationContext.Fork();
                    await MigrationTable(fordedContext, table);
                });
                return Task.CompletedTask;
            }

            IEnumerable<TableDescriptorMapper> OrderByDependency(List<TableDescriptorMapper> tableDescriptors)
            {
                var cloneTables = tableDescriptors.ToArray().ToList();
                List<TableDescriptorMapper> results = new List<TableDescriptorMapper>();
                HashSet<string> resultKeys = new HashSet<string>();
                while (cloneTables.Count > 0)
                {
                    var picked = cloneTables.Where(AllDependenciesOk).ToArray();
                    if (picked.Length == 0)
                    {
                        // dependency loop, A->B, B->C, C->A, in this case can't handle
                        // TOTO REPORT WARNing
                        migrationContext.RaiseWarning($"Cyclic dependence in tables [{BuildTableNames(cloneTables)}]");
                        AddResults(cloneTables.ToArray());
                        break;
                    }
                    else
                    {
                        AddResults(picked);
                    }
                }
                Debug.Assert(results.Count == tableDescriptors.Count);
                return results;

                bool AllDependenciesOk(TableDescriptorMapper tableMapper)
                {
                    var table = tableMapper.Target;
                    if (table.ForeignKeys == null || table.ForeignKeys.Count == 0)
                    {
                        return true;
                    }

                    var tableKey = TableKey(table.Schema, table.Name);
                    return table.ForeignKeys.All(p =>
                    {
                        var dependencyKey = TableKey(p.PrincipalSchema, p.PrincipalTable);
                        return dependencyKey == tableKey || resultKeys.Contains(dependencyKey);
                    });
                }

                string TableKey(string schema, string name) => string.IsNullOrEmpty(schema) ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"";

                void AddResults(IEnumerable<TableDescriptorMapper> pickedTables)
                {
                    pickedTables.Each(p =>
                    {
                        results.Add(p);
                        resultKeys.Add(TableKey(p.Target.Schema, p.Target.Name));
                        cloneTables.Remove(p);
                    });
                }

                string BuildTableNames(IEnumerable<TableDescriptorMapper> tables) => string.Join(",", tables.Select(p => TableKey(p.Target.Schema, p.Target.Name)));
            }
        }

        protected virtual Task ApplyCustomScripts(MigrationContext migrationContext)
        {
            var migrationSetting = migrationContext.Setting;
            if (!string.IsNullOrEmpty(migrationSetting.PostScript?.SqlFile))
            {
                migrationContext.TargetConnection.ExecuteSqlScriptFile(migrationSetting.PostScript.SqlFile, migrationSetting.PostScript.SqlSplit);
            }
            return Task.CompletedTask;
        }

        protected virtual async Task MigrationTable(MigrationContext migrationContext, TableDescriptorMapper tableMapper)
        {
            var migrationSetting = migrationContext.Setting;
            var (source, target) = (migrationContext.Source, migrationContext.Target);
            await target.Agent.DataMigrator.BeforeWriteTargetTable(tableMapper.Target, migrationContext);
            var totalCount = await source.Agent.DataMigrator.CountSourceTable(tableMapper.Source, migrationContext);
            var (migratedCount, maxRowSize, fetchCount) = (0, 1, 1);
            var targetTableFullName = migrationContext.Target.Agent.AgentSetting.IdentityName(tableMapper.Target.Schema, tableMapper.Target.Name);

            while (totalCount > 0)
            {

                var pageInfo = new PageInfo { Offset = migratedCount, Limit = Math.Max(1, fetchCount) };
                var srcDataTable = await source.Agent.DataMigrator.ReadSourceTable(tableMapper.Source, pageInfo, migrationContext);

                var targetDataTable = await _tableDataMapper.MapDataTable(srcDataTable, tableMapper, migrationSetting);
                await target.Agent.DataMigrator.WriteTargetTable(targetDataTable, tableMapper.Target, migrationContext);

                migratedCount += srcDataTable.Rows.Count;
                totalCount = Math.Max(totalCount, migratedCount);
                maxRowSize = Math.Max(maxRowSize, srcDataTable.MaxRowSize());
                fetchCount = Math.Min(fetchCount * migrationSetting.GrowthSpeed, Math.Max(1, migrationSetting.FetchDataMaxSize / maxRowSize));

                if (srcDataTable.Rows.Count < pageInfo.Limit)
                {
                    migrationContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, migratedCount, migratedCount, false);
                    break;
                }
                migrationContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, totalCount, migratedCount, false);
            }
            await target.Agent.DataMigrator.AfterWriteTargetTable(tableMapper.Target, migrationContext);

            migrationContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, migratedCount, migratedCount, true);

        }

    }
}
