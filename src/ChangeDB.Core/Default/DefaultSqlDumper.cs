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

namespace ChangeDB.Default
{
    public class DefaultSqlDumper : IDatabaseSqlDumper
    {

        protected IAgentFactory AgentFactory { get; }
        protected static Action<string> Log = (a) => { };
        public DefaultSqlDumper(IAgentFactory agentFactory)
        {
            AgentFactory = agentFactory;
        }

        public async Task DumpSql(DumpContext context)
        {
            var sourceAgent = AgentFactory.CreateAgent(context.SourceDatabase.DatabaseType);
            var targetAgent = AgentFactory.CreateAgent(context.DumpInfo.DatabaseType);
            await using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);
            // var createNew = context.Setting.DropTargetDatabaseIfExists;
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
            await ApplyCustomScripts(context, context?.Setting?.PreScript);
            await DoDumpDatabase(context);
            await ApplyCustomScripts(context, context?.Setting?.PostScript);
            // flush to file
            await sqlWriter.FlushAsync();
        }



        protected virtual async Task<DatabaseDescriptor> GetSourceDatabaseDescriptor(IMigrationAgent sourceAgent, DbConnection sourceConnection, MigrationContext migrationContext)
        {
            Log("start getting source database metadata.");
            return await sourceAgent.MetadataMigrator.GetSourceDatabaseDescriptor(migrationContext);
        }

        protected virtual Task ApplyMigrationSettings(MigrationContext migrationContext)
        {
            SettingsApplier.ApplySettingForTarget(migrationContext);
            return Task.CompletedTask;
        }

        protected virtual Task ApplyTargetAgentSettings(MigrationContext migrationContext)
        {
            return SettingsApplier.ApplyAgentSettings(migrationContext);
        }

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

        protected virtual async Task PreDumpMetadata(AgentRunTimeInfo target, DumpContext migrationContext)
        {
            migrationContext.RaiseStageChanged(StageKind.StartingPreMeta);
            await target.Agent.MetadataMigrator.PreMigrateTargetMetadata(target.Descriptor, migrationContext);
            migrationContext.RaiseStageChanged(StageKind.FinishedPreMeta);
        }
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
                await MigrateTableInSingleTask(OrderByDependency(migrationContext.Source.Descriptor.Tables));
            }
            else
            {
                await MigrateTableInSingleTask(migrationContext.Source.Descriptor.Tables);
            }
            migrationContext.EventReporter.RaiseStageChanged(StageKind.FinishedTableData);

            bool NeedOrderByDependency() => migrationContext.Setting.MigrationScope == MigrationScope.Data;


            async Task MigrateTableInSingleTask(IEnumerable<TableDescriptor> tables)
            {
                // single task
                foreach (var sourceTable in tables)
                {
                    await DumpTable(migrationContext, sourceTable);
                }
            }


            IEnumerable<TableDescriptor> OrderByDependency(List<TableDescriptor> tableDescriptors)
            {
                var cloneTables = tableDescriptors.ToArray().ToList();
                List<TableDescriptor> results = new List<TableDescriptor>();
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

                bool AllDependenciesOk(TableDescriptor table)
                {
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

                void AddResults(IEnumerable<TableDescriptor> pickedTables)
                {
                    pickedTables.Each(p =>
                    {
                        results.Add(p);
                        resultKeys.Add(TableKey(p.Schema, p.Name));
                        cloneTables.Remove(p);
                    });
                }

                string BuildTableNames(IEnumerable<TableDescriptor> tables) => string.Join(",", tables.Select(p => TableKey(p.Schema, p.Name)));
            }
        }


        protected virtual Task ApplyCustomScripts(MigrationContext migrationContext, CustomSqlScript customSqlScript)
        {
            if (!string.IsNullOrEmpty(customSqlScript?.SqlFile))
            {
                migrationContext.TargetConnection.ExecuteSqlScriptFile(customSqlScript.SqlFile, customSqlScript.SqlSplit);
            }
            return Task.CompletedTask;
        }

        private async IAsyncEnumerable<DataTable> ConvertToTargetDataTable(IAsyncEnumerable<DataTable> dataTables, MigrationSetting migrationSetting)
        {
            await foreach (var dataTable in dataTables)
            {
                dataTable.Columns.OfType<DataColumn>().Each(p =>
                   p.ColumnName = migrationSetting.TargetNameStyle.ColumnNameFunc(p.ColumnName));
                yield return dataTable;
            }
        }


        protected virtual async Task DumpTable(DumpContext dumpContext, TableDescriptor sourceTable)
        {
            var (source, target) = (dumpContext.Source, dumpContext.Target);
            var migrationSetting = dumpContext.Setting;
            var targetTableName = migrationSetting.TargetNameStyle.TableNameFunc(sourceTable.Name);
            var targetTableDesc = target.Descriptor.GetTable(targetTableName);
            var targetTableFullName = dumpContext.Target.Agent.AgentSetting.IdentityName(targetTableDesc.Schema, targetTableDesc.Name);

            await target.Agent.DataMigrator.BeforeWriteTargetTable(targetTableDesc, dumpContext);
            var sourceCount = await source.Agent.DataMigrator.CountSourceTable(sourceTable, dumpContext);

            var sourceDataTables = source.Agent.DataMigrator.ReadSourceTable(sourceTable, dumpContext);
            var targetDataTables = ConvertToTargetDataTable(sourceDataTables, dumpContext.Setting);
            await target.Agent.DataDumper.WriteTables(targetDataTables, targetTableDesc, dumpContext);

            //while (totalCount > 0)
            //{


            //    var pageInfo = new PageInfo { Offset = migratedCount, Limit = Math.Max(1, fetchCount) };
            //    var dataTable = await source.Agent.DataMigrator.ReadSourceTable(sourceTable, pageInfo, migrationContext);
            //    // convert target column name
            //    dataTable.Columns.OfType<DataColumn>().Each(p =>
            //        p.ColumnName = migrationSetting.TargetNameStyle.ColumnNameFunc(p.ColumnName));
            //    await target.Agent.DataMigrator.WriteTargetTable(dataTable, targetTableDesc, migrationContext);

            //    migratedCount += dataTable.Rows.Count;
            //    totalCount = Math.Max(totalCount, migratedCount);
            //    maxRowSize = Math.Max(maxRowSize, dataTable.MaxRowSize());
            //    fetchCount = Math.Min(fetchCount * migrationSetting.GrowthSpeed, Math.Max(1, migrationSetting.FetchDataMaxSize / maxRowSize));

            //    if (dataTable.Rows.Count < pageInfo.Limit)
            //    {
            //        migrationContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, migratedCount, migratedCount, false);
            //        break;
            //    }
            //    migrationContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, totalCount, migratedCount, false);
            //}
            await target.Agent.DataMigrator.AfterWriteTargetTable(targetTableDesc, dumpContext);
            var targetCount = await target.Agent.DataMigrator.CountSourceTable(sourceTable, dumpContext);
            dumpContext.EventReporter.RaiseTableDataMigrated(targetTableFullName, sourceCount, targetCount, true);
        }



    }
}
