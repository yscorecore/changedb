using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Microsoft.Extensions.Logging;
using YS.Knife;

namespace ChangeDB.Default
{
    [Service]
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
            var sourceAgent = _agentFactory.CreateAgent(context.SourceDatabase.Type);
            var targetAgent = _agentFactory.CreateAgent(context.TargetDatabase.Type);
            using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);
            using var targetConnection = targetAgent.CreateConnection(context.TargetDatabase.ConnectionString);

            Log("start geting source database metadata.");
            var sourceDatabaseDescriptor =
                await sourceAgent.MetadataMigrator.GetDatabaseDescriptor(sourceConnection, context.Setting);
            var targetDatabaseDescriptor = ConvertToTargetDatabaseDescriptor(sourceDatabaseDescriptor, context, sourceAgent, targetAgent);



            var source = new MigrationDataInfo
            {
                Agent = sourceAgent,
                Connection = sourceConnection,
                Descriptor = sourceDatabaseDescriptor,
            };
            var target = new MigrationDataInfo
            {
                Agent = targetAgent,
                Connection = targetConnection,
                Descriptor = targetDatabaseDescriptor
            };

            // do migrate
            await CreateEmptyTargetDatabase(context, targetAgent, targetConnection);

            if (context.Setting.IncludeMeta)
            {
                Log("Start pre migration metadata.");
                await targetAgent.MetadataMigrator.PreMigrate(targetDatabaseDescriptor, targetConnection, context.Setting);
            }

            if (context.Setting.IncludeData)
            {
                Log("start migrating data.");
                await MigrationData(source, target, context);

                Log("all data migration completed.");
            }

            if (context.Setting.IncludeMeta)
            {
                Log("Start post migration metadata.");
                await targetAgent.MetadataMigrator.PostMigrate(targetDatabaseDescriptor, targetConnection, context.Setting);

            }
            Log("migration succeeded.");
        }

        private async Task CreateEmptyTargetDatabase(MigrationContext context, IMigrationAgent targetAgent, DbConnection targetConnection)
        {
            if (context.Setting.DropTargetDatabaseIfExists)
            {
                Log("dropping target database if exists.");
                await targetAgent.DatabaseManger.DropDatabaseIfExists(targetConnection, context.Setting);
            }
            Log("creating target database.");
            await targetAgent.DatabaseManger.CreateDatabase(targetConnection, context.Setting);
        }

        private DatabaseDescriptor ConvertToTargetDatabaseDescriptor(DatabaseDescriptor databaseDescriptor, MigrationContext migrationContext, IMigrationAgent sourceAgent, IMigrationAgent targetAgent)
        {
            var sameDatabaseType = string.Equals(migrationContext.SourceDatabase?.Type, migrationContext.TargetDatabase?.Type, StringComparison.InvariantCultureIgnoreCase);
            var clonedDescriptor = databaseDescriptor.DeepClone();
            // TODO apply filter
            ApplyNamingRules();
            ConvertDataTypes();
            TranslateSqlExpressions();
            return clonedDescriptor;

            void ConvertDataTypes()
            {
                if (!sameDatabaseType)
                {
                    clonedDescriptor.Tables.SelectMany(p => p.Columns).ForEach(column =>
                    {
                        var commonType = sourceAgent.DataTypeMapper.ToCommonDatabaseType(column.StoreType);
                        column.StoreType = targetAgent.DataTypeMapper.ToDatabaseStoreType(commonType);
                    });

                    clonedDescriptor.Sequences.ForEach(sequence =>
                    {
                        var commonType = sourceAgent.DataTypeMapper.ToCommonDatabaseType(sequence.StoreType);
                        sequence.StoreType = targetAgent.DataTypeMapper.ToDatabaseStoreType(commonType);
                    });
                }
            }

            void ApplyNamingRules()
            {
                var nameStyle = migrationContext.Setting.TargetNameStyle;
                Func<string, string> columnConvertFunc = nameStyle.ColumnNameFunc;
                Func<string, string> tableConvertFunc = nameStyle.TableNameFunc;
                Func<string, string> schemaConvertFunc = nameStyle.SchemaNameFunc;
                Func<string, string> sequenceConvertFunc = nameStyle.SequenceNameFunc;
                Func<string, string> indexConvertFunc = nameStyle.IndexNameFunc;
                Func<string, string> uniqueConvertFunc = nameStyle.UniqueNameFunc;
                Func<string, string> foreignKeyConvertFunc = nameStyle.ForeignKeyNameFunc;
                Func<string, string> primaryKeyConvertFunc = nameStyle.PrimaryKeyNameFunc;
                foreach (var table in clonedDescriptor.Tables)
                {
                    table.Schema = schemaConvertFunc(table.Schema);
                    table.Name = schemaConvertFunc(table.Name);

                    foreach (var column in table.Columns)
                    {
                        column.Name = columnConvertFunc(column.Name);
                    }
                    table.PrimaryKey.DoIfNotNull(primaryKey =>
                    {
                        primaryKey.Name = primaryKeyConvertFunc(primaryKey.Name);
                        primaryKey.Columns = primaryKey.Columns.Select(columnConvertFunc).ToList();
                    });

                    foreach (var index in table.Indexes)
                    {
                        index.Name = indexConvertFunc(index.Name);
                        index.Columns = index.Columns.Select(columnConvertFunc).ToList();
                    }
                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        foreignKey.Name = foreignKeyConvertFunc(foreignKey.Name);
                        foreignKey.PrincipalSchema = schemaConvertFunc(foreignKey.PrincipalSchema);
                        foreignKey.PrincipalTable = tableConvertFunc(foreignKey.PrincipalTable);
                        foreignKey.ColumnNames = foreignKey.ColumnNames.Select(columnConvertFunc).ToList();
                        foreignKey.PrincipalNames = foreignKey.PrincipalNames.Select(columnConvertFunc).ToList();
                    }
                    foreach (var unique in table.Uniques)
                    {
                        unique.Name = uniqueConvertFunc(unique.Name);
                        unique.Columns = unique.Columns.Select(columnConvertFunc).ToList();
                    }
                }
                foreach (var sequence in clonedDescriptor.Sequences)
                {
                    sequence.Schema = schemaConvertFunc(sequence.Schema);
                    sequence.Name = sequenceConvertFunc(sequence.Name);
                }

            }

            void TranslateSqlExpressions()
            {
                if (!sameDatabaseType)
                {
                    clonedDescriptor.Tables.SelectMany(p => p.Columns)
                        .ForEach(column =>
                    {
                        if (!string.IsNullOrEmpty(column.DefaultValueSql))
                        {
                            var commonExpression = sourceAgent.ExpressionTranslator.ToCommonSqlExpression(column.DefaultValueSql);
                            column.DefaultValueSql = targetAgent.ExpressionTranslator.FromCommonSqlExpression(commonExpression);
                        }
                        if (!string.IsNullOrEmpty(column.ComputedColumnSql))
                        {
                            var commonExpression = sourceAgent.ExpressionTranslator.ToCommonSqlExpression(column.ComputedColumnSql);
                            column.ComputedColumnSql = targetAgent.ExpressionTranslator.FromCommonSqlExpression(commonExpression);
                        }
                    });
                }
            }
        }
        protected virtual async Task MigrationData(MigrationDataInfo source, MigrationDataInfo target, MigrationContext context)
        {
            foreach (var sourceTable in source.Descriptor.Tables)
            {
                var targetTableName = context.Setting.TargetNameStyle.ColumnNameFunc(sourceTable.Name);
                var targetTableDesc = target.Descriptor.GetTable(targetTableName);
                await target.Agent.DataMigrator.BeforeWriteTableData(targetTableDesc, target.Connection, context.Setting);
                //var tableName = string.IsNullOrEmpty(sourceTable.Schema) ? sourceTable.Name : $"{sourceTable.Schema}.{sourceTable.Name}";
                //_logger.LogInformation($"migrating data of table {tableName}.");
                var totalCount = await source.Agent.DataMigrator.CountTable(sourceTable, source.Connection, context.Setting);
                var migratedCount = 0;
                while (true)
                {
                    var pageInfo = new PageInfo { Offset = migratedCount, Limit = context.Setting.MaxPageSize };
                    var sourceTableData = await source.Agent.DataMigrator.ReadTableData(sourceTable, pageInfo, source.Connection, context.Setting);

                    var targetTableData = UseNamingRules(sourceTableData, context.Setting.TargetNameStyle.ColumnNameFunc);

                    await target.Agent.DataMigrator.WriteTableData(targetTableData, targetTableDesc, target.Connection, context.Setting);
                    if (sourceTableData.Rows.Count < pageInfo.Limit)
                    {
                        // end of table
                        break;
                    }
                    else
                    {
                        migratedCount += sourceTableData.Rows.Count;
                    }
                }
                await target.Agent.DataMigrator.AfterWriteTableData(targetTableDesc, target.Connection, context.Setting);
            }

        }
        private static DataTable UseNamingRules(DataTable table, Func<string, string> columnNamingFunc)
        {
            foreach (DataColumn column in table.Columns)
            {
                column.ColumnName = columnNamingFunc(column.ColumnName);
            }
            return table;
        }


        protected record MigrationDataInfo
        {
            public IMigrationAgent Agent { get; init; }
            public DatabaseDescriptor Descriptor { get; init; }
            public DbConnection Connection { get; init; }
        }

    }
}
