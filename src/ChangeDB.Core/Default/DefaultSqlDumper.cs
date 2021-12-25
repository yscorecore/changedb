using System.Collections.Generic;
using System.Diagnostics;
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


            context.Source = new AgentRunTimeInfo
            {
                Agent = sourceAgent,
                //Connection = sourceConnection,
                Descriptor = null,
            };
            var sourceDatabaseDescriptor = await GetSourceDatabaseDescriptor(sourceAgent, sourceConnection, context);

            context.Source.Descriptor = sourceDatabaseDescriptor;

            context.Target = new AgentRunTimeInfo
            {
                Agent = targetAgent,
                //Connection = new Fakes.SqlScriptDbConnection(context.DumpInfo.SqlScriptFile, (val) => targetAgent.Repr.ReprValue(val)),
                Descriptor = sourceDatabaseDescriptor.DeepClone(),
            };


            await ApplyMigrationSettings(context);
            await ApplyTargetAgentSettings(context);
            await DoMigrateDatabase(context);
            await ApplyCustomScripts(context);

            Log($"dump to file '{context.DumpInfo.SqlScriptFile}' succeeded.");

        }

        protected override async Task DoMigrateDatabase(MigrationContext migrationContext)
        {
            var (target, source, migrationSetting) = (migrationContext.Target, migrationContext.Source, migrationContext.Setting);

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

        protected override async Task MigrationData(MigrationContext migrationContext)
        {
            if (migrationContext.Setting.MigrationType == MigrationType.Data)
            {
                migrationContext.EventReporter.RaiseStageChanged(StageKind.StartingTableData);
                var sortedTables = OrderByDependency(migrationContext, migrationContext.Source.Descriptor.Tables);
                foreach (var table in sortedTables)
                {
                    await base.MigrationTable(migrationContext, table);
                }
                migrationContext.EventReporter.RaiseStageChanged(StageKind.FinishedTableData);
            }
            else
            {
                await base.MigrationData(migrationContext);
            }
        }

        private List<TableDescriptor> OrderByDependency(MigrationContext migrationContext, List<TableDescriptor> tableDescriptors)
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

            string BuildTableNames(IEnumerable<TableDescriptor> tables)
            {
                return string.Join(",", tables.Select(p => TableKey(p.Schema, p.Name)));
            }
        }
    }
}
