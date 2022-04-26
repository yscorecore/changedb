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



            await using var sourceAgentContext = new AgentContext
            {
                Agent = sourceAgent,
                Connection = sourceAgent.ConnectionProvider.CreateConnection(context.SourceDatabase.ConnectionString),
                ConnectionString = context.SourceDatabase.ConnectionString,
                EventReporter = null
            };

            await using var targetAgentContext = new AgentContext
            {
                Agent = targetAgent,
                Connection = targetAgent.ConnectionProvider.CreateConnection(context.TargetDatabase.ConnectionString),
                ConnectionString = context.TargetDatabase.ConnectionString,
                EventReporter = null
            };


            var sourceDatabaseDescriptor = await sourceAgent.MetadataMigrator.GetDatabaseDescriptor(sourceAgentContext);

            var databaseDescriptorMapper =
                await _databaseMapper.MapDatabase(sourceDatabaseDescriptor, targetAgent.AgentSetting, context.Setting);


            await DoMigrateDatabase(context.Setting, databaseDescriptorMapper, sourceAgentContext, targetAgentContext);
            await ApplyCustomScripts(context.Setting, targetAgentContext);

        }


        protected virtual async Task DoMigrateDatabase(MigrationSetting migrationSetting, DatabaseDescriptorMapper mapper, AgentContext sourceContext, AgentContext targetContext)
        {
            if (migrationSetting.IncludeMeta)
            {
                await CreateTargetDatabase(migrationSetting, targetContext);
            }

            if (migrationSetting.IncludeMeta)
            {
                await PreMigrationMetadata(mapper.Target, targetContext);
            }

            if (migrationSetting.IncludeData)
            {
                await MigrationData(migrationSetting, mapper, sourceContext, targetContext);
            }

            if (migrationSetting.IncludeMeta)
            {
                await PostMigrationMetadata(mapper.Target, targetContext);
            }
        }

        protected virtual async Task CreateTargetDatabase(MigrationSetting migrationSetting, AgentContext migrationContext)
        {
            var (targetAgent, targetConnection) = (migrationContext.Agent, migrationContext.Connection);
            if (migrationSetting.DropTargetDatabaseIfExists)
            {
                Log("dropping target database if exists.");
                await targetAgent.DatabaseManger.DropTargetDatabaseIfExists(migrationContext.Connection.ConnectionString, migrationSetting);
            }
            Log("creating target database.");
            await targetAgent.DatabaseManger.CreateDatabase(migrationContext.ConnectionString, migrationSetting);
        }

        protected virtual async Task PreMigrationMetadata(DatabaseDescriptor target, AgentContext agentContext)
        {
            agentContext.RaiseStageChanged(StageKind.StartingPreMeta);
            await agentContext.Agent.MetadataMigrator.PreMigrateMetadata(target, agentContext);
            agentContext.RaiseStageChanged(StageKind.FinishedPreMeta);
        }

        protected virtual async Task PostMigrationMetadata(DatabaseDescriptor target, AgentContext agentContext)
        {
            agentContext.RaiseStageChanged(StageKind.StartingPostMeta);
            await agentContext.Agent.MetadataMigrator.PostMigrateMetadata(target, agentContext);
            agentContext.RaiseStageChanged(StageKind.FinishedPostMeta);
        }

        protected virtual async Task MigrationData(MigrationSetting migrationSetting, DatabaseDescriptorMapper mapper, AgentContext sourceContext, AgentContext targetContext)
        {
            targetContext.RaiseStageChanged(StageKind.StartingTableData);
            if (NeedOrderByDependency())
            {
                await MigrateTableInSingleTask(OrderByDependency(mapper.TableMappers));
            }
            else
            {
                if (NeedMultiTask())
                {
                    await MigrateTableInParallelTasks();
                }
                else
                {
                    await MigrateTableInSingleTask(mapper.TableMappers);
                }
            }
            targetContext.RaiseStageChanged(StageKind.FinishedTableData);

            bool NeedOrderByDependency() => migrationSetting.MigrationScope == MigrationScope.Data;

            bool NeedMultiTask() => migrationSetting.MaxTaskCount > 1;

            async Task MigrateTableInSingleTask(IEnumerable<TableDescriptorMapper> tableMappers)
            {
                // single task
                foreach (var tableMapper in tableMappers)
                {
                    await MigrationTable(migrationSetting, tableMapper, sourceContext, targetContext);
                }
            }
            Task MigrateTableInParallelTasks()
            {
                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = migrationSetting.MaxTaskCount
                };
                _ = Parallel.ForEach(mapper.TableMappers, options, async (table) =>
                {
                    await using var forkedSourceContext = sourceContext.Fork();
                    await using var forkedTargetContext = targetContext.Fork();
                    await MigrationTable(migrationSetting, table, forkedSourceContext, forkedTargetContext);
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
                        targetContext.RaiseWarning($"Cyclic dependence in tables [{BuildTableNames(cloneTables)}]");
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



        protected virtual Task ApplyCustomScripts(MigrationSetting migrationSetting, AgentContext agentContext)
        {
            if (!string.IsNullOrEmpty(migrationSetting.PostScript?.SqlFile))
            {
                // TODO use sqlimporter
                agentContext.Connection.ExecuteSqlScriptFile(migrationSetting.PostScript.SqlFile, migrationSetting.PostScript.SqlSplit);
            }
            return Task.CompletedTask;
        }

        private async IAsyncEnumerable<DataTable> ConvertToTargetDataTable(IAsyncEnumerable<DataTable> dataTables, TableDescriptorMapper tableDescriptorMapper, MigrationSetting migrationSetting)
        {
            await foreach (var dataTable in dataTables)
            {
                yield return await _tableDataMapper.MapDataTable(dataTable, tableDescriptorMapper, migrationSetting);
            }
        }

        protected virtual async Task MigrationTable(MigrationSetting migrationSetting, TableDescriptorMapper tableMapper, AgentContext sourceContext, AgentContext targetContext)
        {
            var targetTableFullName = targetContext.Agent.AgentSetting.IdentityName(tableMapper.Target.Schema, tableMapper.Target.Name);
            await targetContext.Agent.DataMigrator.BeforeWriteTargetTable(tableMapper.Target, targetContext);

            var sourceCount = await sourceContext.Agent.DataMigrator.CountSourceTable(tableMapper.Source, sourceContext);
            var sourceDataTables = sourceContext.Agent.DataMigrator.ReadSourceTable(tableMapper.Source, sourceContext, migrationSetting);
            var targetDataTables = ConvertToTargetDataTable(sourceDataTables, tableMapper, migrationSetting);
            await targetContext.Agent.DataMigrator.WriteTargetTable(targetDataTables, tableMapper.Target, targetContext, InsertionKind.Default);


            var targetCount = await targetContext.Agent.DataMigrator.CountSourceTable(tableMapper.Target, targetContext);
            await targetContext.Agent.DataMigrator.AfterWriteTargetTable(tableMapper.Target, targetContext);

            targetContext.RaiseTableDataMigrated(targetTableFullName, targetCount, targetCount, true);

        }

    }
}
