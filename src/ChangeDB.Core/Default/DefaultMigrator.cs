using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
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
        public async Task MigrateDatabase(MigrationContext context)
        {
            var sourceAgent = _agentFactory.CreateAgent(context.SourceDatabase.Type);
            var targetAgent = _agentFactory.CreateAgent(context.TargetDatabase.Type);
            using var sourceConnection = sourceAgent.CreateConnection(context.SourceDatabase.ConnectionString);
            using var targetConnection = targetAgent.CreateConnection(context.TargetDatabase.ConnectionString);


            var sourceDatabaseDescriptor =
                await sourceAgent.MetadataMigrator.GetDatabaseDescriptor(sourceConnection, context.Setting);

            var targetDatabaseDescriptor = ConvertToTargetDatabaseDescriptor(sourceDatabaseDescriptor, context, sourceAgent, targetAgent);

            // do migrate
            await CreateEmptyTargetDatabase(context, targetAgent, targetConnection);

            if (context.Setting.IncludeMeta)
            {
                await targetAgent.MetadataMigrator.PreMigrate(targetDatabaseDescriptor, targetConnection, context.Setting);
            }

            if (context.Setting.IncludeData)
            {
                await MigrationData(sourceAgent, targetAgent, sourceDatabaseDescriptor, context, sourceConnection, targetConnection);
            }

            if (context.Setting.IncludeMeta)
            {
                await targetAgent.MetadataMigrator.PostMigrate(targetDatabaseDescriptor, targetConnection, context.Setting);

            }
        }

        private async Task CreateEmptyTargetDatabase(MigrationContext context, IMigrationAgent targetAgent, DbConnection targetConnection)
        {
            if (context.Setting.DropTargetDatabaseIfExists)
            {
                await targetAgent.DatabaseManger.DropDatabaseIfExists(targetConnection, context.Setting);
            }

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
                Func<string, string> columnConvertFunc = (p) => p;
                Func<string, string> tableConvertFunc = (p) => p;
                Func<string, string> schemaConvertFunc = (p) => p;
                Func<string, string> sequenceConvertFunc = (p) => p;
                Func<string, string> indexConvertFunc = (p) => p;
                Func<string, string> uniqueConvertFunc = (p) => p;
                Func<string, string> foreignKeyConvertFunc = (p) => p;
                Func<string, string> primaryKeyConvertFunc = (p) => p;
                foreach (var table in clonedDescriptor.Tables)
                {
                    table.Schema = schemaConvertFunc(table.Schema);
                    table.Name = schemaConvertFunc(table.Name);

                    foreach (var column in table.Columns)
                    {
                        column.Name = columnConvertFunc(column.Name);
                    }
                    if (table.PrimaryKey != null)
                    {
                        table.PrimaryKey.Name = primaryKeyConvertFunc(table.PrimaryKey.Name);
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


        protected virtual async Task MigrationData(IMigrationAgent source, IMigrationAgent target, DatabaseDescriptor database, MigrationContext context, DbConnection sourceConnection, DbConnection targetConnection)
        {
            foreach (var tableDescriptor in database.Tables)
            {
                var totalCount = await source.DataMigrator.CountTable(tableDescriptor, sourceConnection, context.Setting);
                var migratedCount = 0;
                while (true)
                {
                    var pageInfo = new PageInfo { Offset = migratedCount, Limit = context.Setting.MaxPageSize };
                    var sourceTableData = await source.DataMigrator.ReadTableData(tableDescriptor, pageInfo, sourceConnection, context.Setting);
                    await target.DataMigrator.WriteTableData(sourceTableData, tableDescriptor, targetConnection, context.Setting);
                    if (sourceTableData.Rows.Count < context.Setting.MaxPageSize)
                    {
                        // end of table
                        break;
                    }
                    else
                    {
                        migratedCount += sourceTableData.Rows.Count;
                    }
                }
            }
        }

    }
}
