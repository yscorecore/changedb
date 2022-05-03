using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Default
{
    public class DefaultSqlDumper : IDatabaseSqlDumper
    {
        private IDatabaseMapper _databaseMapper;

        private ITableDataMapper _tableDataMapper;
        protected IAgentFactory AgentFactory { get; }
        protected static Action<string> Log = (a) => { };
        public DefaultSqlDumper(IAgentFactory agentFactory, IDatabaseMapper databaseMapper, ITableDataMapper tableDataMapper)
        {
            AgentFactory = agentFactory;
            _databaseMapper = databaseMapper;
            _tableDataMapper = tableDataMapper;
        }

        [Obsolete]
        public async Task DumpSql(DumpContext context)
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
                Connection = new SqlScriptDbConnection(context.Writer),
                ConnectionString = string.Empty,
                EventReporter = null
            };


            var sourceDatabaseDescriptor = await sourceAgent.MetadataMigrator.GetDatabaseDescriptor(sourceAgentContext);

            var databaseDescriptorMapper =
                await _databaseMapper.MapDatabase(sourceDatabaseDescriptor, targetAgent.AgentSetting, context.Setting);



            await DoDumpDatabase(context.Setting, databaseDescriptorMapper, sourceAgentContext, targetAgentContext);
            await ApplyCustomScripts(context);
            // flush to file
            await context.Writer.FlushAsync();
        }

        [Obsolete]
        protected virtual async Task DoDumpDatabase(MigrationSetting setting, DatabaseDescriptorMapper mapper, AgentContext sourceContext, AgentContext targetContext)
        {

            if (setting.IncludeMeta)
            {
                await PreDumpMetadata(mapper.Target, targetContext);
            }

            if (setting.IncludeData)
            {
                await DumpData(setting, mapper, sourceContext, targetContext);
            }

            if (setting.IncludeMeta)
            {
                await PostDumpMetadata(mapper.Target, targetContext);
            }
        }


        protected virtual async Task PreDumpMetadata(DatabaseDescriptor target, AgentContext agentContext)
        {
            agentContext.RaiseStageChanged(StageKind.StartingPreMeta);
            await agentContext.Agent.MetadataMigrator.PreMigrateMetadata(target, agentContext);
            agentContext.RaiseStageChanged(StageKind.FinishedPreMeta);
        }

        protected virtual async Task PostDumpMetadata(DatabaseDescriptor target, AgentContext agentContext)
        {
            agentContext.RaiseStageChanged(StageKind.StartingPostMeta);
            await agentContext.Agent.MetadataMigrator.PostMigrateMetadata(target, agentContext);
            agentContext.RaiseStageChanged(StageKind.FinishedPostMeta);
        }



        [Obsolete]
        protected virtual async Task DumpData(MigrationSetting migrationSetting, DatabaseDescriptorMapper mapper, AgentContext sourceContext, AgentContext targetContext)
        {
            targetContext.RaiseStageChanged(StageKind.StartingTableData);
            if (NeedOrderByDependency())
            {
                await MigrateTableInSingleTask(OrderByDependency(mapper.TableMappers));
            }
            else
            {
                await MigrateTableInSingleTask(mapper.TableMappers);
            }
            targetContext.RaiseStageChanged(StageKind.FinishedTableData);

            bool NeedOrderByDependency() => migrationSetting.MigrationScope == MigrationScope.Data;


            async Task MigrateTableInSingleTask(IEnumerable<TableDescriptorMapper> tables)
            {
                // single task
                foreach (var tableMapper in tables)
                {
                    await DumpTable(migrationSetting, tableMapper, sourceContext, targetContext);
                }
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


        protected virtual Task ApplyCustomScripts(DumpContext dumpContext)
        {
            var migrationSetting = dumpContext.Setting;
            if (!string.IsNullOrEmpty(migrationSetting.PostScript?.SqlFile))
            {
                if (File.Exists(migrationSetting.PostScript.SqlFile))
                {
                    using var reader = new StreamReader(migrationSetting.PostScript.SqlFile);
                    dumpContext.Writer.AppendReader(reader);
                }
                else
                {
                    throw new ChangeDBException($"script file '{migrationSetting.PostScript.SqlFile}' not found.");
                }
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

        [Obsolete]
        protected virtual async Task DumpTable(MigrationSetting migrationSetting, TableDescriptorMapper tableMapper, AgentContext sourceContext, AgentContext targetContext)
        {
            var targetTableFullName = targetContext.Agent.AgentSetting.IdentityName(tableMapper.Target.Schema, tableMapper.Target.Name);

            await targetContext.Agent.DataMigrator.BeforeWriteTargetTable(tableMapper.Target, targetContext);

            var sourceCount = await sourceContext.Agent.DataMigrator.CountSourceTable(tableMapper.Source, sourceContext);
            var sourceDataTables = sourceContext.Agent.DataMigrator.ReadSourceTable(tableMapper.Source, sourceContext, migrationSetting);
            var targetDataTables = ConvertToTargetDataTable(sourceDataTables, tableMapper, migrationSetting);
            await targetContext.Agent.DataDumper.WriteTables(targetDataTables, tableMapper.Target, null);


            await targetContext.Agent.DataMigrator.AfterWriteTargetTable(tableMapper.Target, targetContext);
            targetContext.RaiseTableDataMigrated(targetTableFullName, sourceCount, sourceCount, true);
        }



    }
}
