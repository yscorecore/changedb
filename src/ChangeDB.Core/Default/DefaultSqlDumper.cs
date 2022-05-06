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

        public async Task DumpSql(DumpSetting setting, IEventReporter eventReporter)
        {
            var sourceAgent = AgentFactory.CreateAgent(setting.SourceDatabase.DatabaseType);
            var targetAgent = AgentFactory.CreateAgent(setting.TargetDatabase.DatabaseType);

            await using var sourceAgentContext = new DumpContext
            {
                Agent = sourceAgent,
                Connection = sourceAgent.ConnectionProvider.CreateConnection(setting.SourceDatabase.ConnectionString),
                ConnectionString = setting.SourceDatabase.ConnectionString,
                EventReporter = eventReporter,
                Setting = setting,
            };

            await using var targetAgentContext = new DumpContext
            {
                Agent = targetAgent,
                Connection = new SqlScriptDbConnection(setting.Writer),
                ConnectionString = string.Empty,
                EventReporter = eventReporter,
                Setting = setting,
            };


            var sourceDatabaseDescriptor = await sourceAgent.MetadataMigrator.GetDatabaseDescriptor(sourceAgentContext);

            var databaseDescriptorMapper =
                await _databaseMapper.MapDatabase(sourceDatabaseDescriptor, targetAgent.AgentSetting, setting);


            await ApplyPreScripts(setting);
            await DoDumpDatabase(setting, databaseDescriptorMapper, sourceAgentContext, targetAgentContext);
            await ApplyPostScripts(setting);
            // flush to file
            await setting.Writer.FlushAsync();
        }

        protected virtual async Task DoDumpDatabase(DumpSetting setting, DatabaseDescriptorMapper mapper, AgentContext sourceContext, AgentContext targetContext)
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


/* Unmerged change from project 'ChangeDB.Core(net6)'
Added:
        [Obsolete]
*/
        [Obsolete]
        protected virtual async Task DumpData(DumpSetting migrationSetting, DatabaseDescriptorMapper mapper, AgentContext sourceContext, AgentContext targetContext)
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

        protected virtual Task ApplyPreScripts(DumpSetting dumpSetting)
        {
            var migrationSetting = dumpSetting;
            if (!string.IsNullOrEmpty(migrationSetting.PreScript?.SqlFile))
            {
                using var reader = new StreamReader(migrationSetting.PreScript.SqlFile);
                dumpSetting.Writer.AppendReader(reader);
            }
            return Task.CompletedTask;
        }

        protected virtual Task ApplyPostScripts(DumpSetting dumpSetting)
        {
            var migrationSetting = dumpSetting;
            if (!string.IsNullOrEmpty(migrationSetting.PostScript?.SqlFile))
            {
                using var reader = new StreamReader(migrationSetting.PostScript.SqlFile);
                dumpSetting.Writer.AppendReader(reader);
            }
            return Task.CompletedTask;
        }

        private async IAsyncEnumerable<DataTable> ConvertToTargetDataTable(IAsyncEnumerable<DataTable> dataTables, TableDescriptorMapper tableDescriptorMapper, DumpSetting migrationSetting)
        {
            await foreach (var dataTable in dataTables)
            {
                yield return await _tableDataMapper.MapDataTable(dataTable, tableDescriptorMapper, migrationSetting);
            }
        }

        [Obsolete]
        protected virtual async Task DumpTable(DumpSetting migrationSetting, TableDescriptorMapper tableMapper, AgentContext sourceContext, AgentContext targetContext)
        {
            var targetTableFullName = targetContext.Agent.AgentSetting.IdentityName(tableMapper.Target.Schema, tableMapper.Target.Name);

            await targetContext.Agent.DataMigrator.BeforeWriteTable(tableMapper.Target, targetContext);

            var sourceCount = await sourceContext.Agent.DataMigrator.CountSourceTable(tableMapper.Source, sourceContext);
            var sourceDataTables = sourceContext.Agent.DataMigrator.ReadSourceTable(tableMapper.Source, sourceContext, migrationSetting);
            var targetDataTables = ConvertToTargetDataTable(sourceDataTables, tableMapper, migrationSetting);
            await targetContext.Agent.DataDumper.WriteTables(targetDataTables, tableMapper.Target, null);


            await targetContext.Agent.DataMigrator.AfterWriteTable(tableMapper.Target, targetContext);
            targetContext.RaiseTableDataMigrated(targetTableFullName, sourceCount, sourceCount, true);
        }



    }
}
