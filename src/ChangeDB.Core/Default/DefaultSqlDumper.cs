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
            await using var sourceConnection = sourceAgent.ConnectionProvider.CreateConnection(context.SourceDatabase.ConnectionString);
            var createNew = context.Setting.DropTargetDatabaseIfExists;
            await using var targetConnection = new SqlScriptDbConnection(context.Writer);
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


            await DoDumpDatabase(context);
            await ApplyCustomScripts(context);
            // flush to file
            await context.Writer.FlushAsync();
        }

        [Obsolete]
        protected virtual async Task DoDumpDatabase(DumpContext dumpContext)
        {
            var (target, source, migrationSetting) = (dumpContext.Target, dumpContext.Source, dumpContext.Setting);


            if (dumpContext.Setting.IncludeMeta)
            {
                await PreDumpMetadata(target, dumpContext);
            }

            if (dumpContext.Setting.IncludeData)
            {
                await DumpData(dumpContext);
            }

            if (dumpContext.Setting.IncludeMeta)
            {
                await PostDumpMetadata(target, dumpContext);
            }
        }

        [Obsolete]
        protected virtual async Task PreDumpMetadata(AgentRunTimeInfo target, DumpContext migrationContext)
        {
            migrationContext.RaiseStageChanged(StageKind.StartingPreMeta);
            await target.Agent.MetadataMigrator.PreMigrateTargetMetadata(target.Descriptor, migrationContext);
            migrationContext.RaiseStageChanged(StageKind.FinishedPreMeta);
        }

        [Obsolete]
        protected virtual async Task PostDumpMetadata(AgentRunTimeInfo target, DumpContext migrationContext)
        {
            migrationContext.RaiseStageChanged(StageKind.StartingPostMeta);
            await target.Agent.MetadataMigrator.PostMigrateTargetMetadata(target.Descriptor, migrationContext);
            migrationContext.RaiseStageChanged(StageKind.FinishedPostMeta);
        }
        protected virtual async Task DumpData(DumpContext migrationContext)
        {
            migrationContext.EventReporter.RaiseStageChanged(StageKind.StartingTableData);
            if (NeedOrderByDependency())
            {
                await MigrateTableInSingleTask(OrderByDependency(migrationContext.DatabaseMapper.TableMappers));
            }
            else
            {
                await MigrateTableInSingleTask(migrationContext.DatabaseMapper.TableMappers);
            }
            migrationContext.EventReporter.RaiseStageChanged(StageKind.FinishedTableData);

            bool NeedOrderByDependency() => migrationContext.Setting.MigrationScope == MigrationScope.Data;


            async Task MigrateTableInSingleTask(IEnumerable<TableDescriptorMapper> tables)
            {
                // single task
                foreach (var tableMapper in tables)
                {
                    await DumpTable(migrationContext, tableMapper);
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
        protected virtual async Task DumpTable(DumpContext dumpContext, TableDescriptorMapper tableMapper)
        {
            var (source, target) = (dumpContext.Source, dumpContext.Target);
            var targetTableFullName = dumpContext.Target.Agent.AgentSetting.IdentityName(tableMapper.Target.Schema, tableMapper.Target.Name);

            await target.Agent.DataMigrator.BeforeWriteTargetTable(tableMapper.Source, dumpContext);
            var sourceCount = await source.Agent.DataMigrator.CountSourceTable(tableMapper.Source, dumpContext);

            var sourceDataTables = source.Agent.DataMigrator.ReadSourceTable(tableMapper.Source, dumpContext);
            var targetDataTables = ConvertToTargetDataTable(sourceDataTables, tableMapper, dumpContext.Setting);
            await target.Agent.DataDumper.WriteTables(targetDataTables, tableMapper.Target, dumpContext);


            await target.Agent.DataMigrator.AfterWriteTargetTable(tableMapper.Target, dumpContext);
            var targetCount = await target.Agent.DataMigrator.CountSourceTable(tableMapper.Target, dumpContext);
            dumpContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, sourceCount, targetCount, true);
        }



    }
}
