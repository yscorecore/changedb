using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Default
{
    class MigrationSettingsApplier
    {

        public static void ApplySettingForTarget(AgentRunTimeInfo source,AgentRunTimeInfo target,MigrationSetting migrationSetting)
        {
            var isSameDbType = string.Equals(source.DatabaseType, target.DatabaseType, StringComparison.InvariantCultureIgnoreCase);
            var clonedDescriptor = target.Descriptor;

            FixDuplicateObjectName();
            ApplyNamingRules();
            ConvertDataTypeAndExpressions();

            void ConvertDataTypeAndExpressions()
            {
                // the same database type
                if (isSameDbType)
                {
                    return;
                }
                ConvertTableDataTypeAndExpression();
                ConvertSequenceDataType();

                void ConvertSequenceDataType()
                {
                    foreach (var sequence in clonedDescriptor.Sequences)
                    {
                        var commonType = source.Agent.DataTypeMapper.ToCommonDatabaseType(sequence.StoreType);
                        sequence.StoreType = target.Agent.DataTypeMapper.ToDatabaseStoreType(commonType);
                    }
                }
                void ConvertTableDataTypeAndExpression()
                {
                    foreach (var table in clonedDescriptor.Tables)
                    {
                        foreach (var column in table.Columns)
                        {
                            var sourceDataType = column.StoreType;
                            var commonType = source.Agent.DataTypeMapper.ToCommonDatabaseType(sourceDataType);
                            var targetDataType = target.Agent.DataTypeMapper.ToDatabaseStoreType(commonType);

                            column.StoreType = targetDataType;

                            var sourceContext = new SqlExpressionTranslatorContext
                            {
                                StoreType = sourceDataType,
                                AgentInfo = source
                            };

                            var targetContext = new SqlExpressionTranslatorContext
                            {
                                StoreType = targetDataType,
                                AgentInfo = target
                            };
                            if (!string.IsNullOrEmpty(column.DefaultValueSql))
                            {
                                var commonExpression = source.Agent.ExpressionTranslator.ToCommonSqlExpression(column.DefaultValueSql, sourceContext);
                                column.DefaultValueSql = target.Agent.ExpressionTranslator.FromCommonSqlExpression(commonExpression, targetContext);
                            }
                            if (!string.IsNullOrEmpty(column.ComputedColumnSql))
                            {
                                var commonExpression = source.Agent.ExpressionTranslator.ToCommonSqlExpression(column.DefaultValueSql, sourceContext);
                                column.ComputedColumnSql = target.Agent.ExpressionTranslator.FromCommonSqlExpression(commonExpression, targetContext);
                            }
                        }
                    }
                }

            }

            void FixDuplicateObjectName()
            {
                if (isSameDbType)
                {
                    return;
                }
                var objectDics = clonedDescriptor.Tables.ToDictionary(p => ObjectCacheName(p.Schema, p.Name), p => new List<INameObject>());
                foreach (var table in clonedDescriptor.Tables)
                {
                    table.PrimaryKey.DoIfNotNull(p => AppendObject(table.Schema, p));
                    table.Uniques.Each(p => AppendObject(table.Schema, p));
                    table.ForeignKeys.Each(p => AppendObject(table.Schema, p));
                    table.Indexes.Each(p => AppendObject(table.Schema, p));
                }
                objectDics.Values.Each(p => p.Each((t, i) => { t.Name = $"{t.Name}_{i + 1}"; }));
                void AppendObject(string schema, INameObject nameObject)
                {
                    if (string.IsNullOrEmpty(nameObject?.Name))
                    {   // don't handle empty name
                        return;
                    }
                    var cacheKey = ObjectCacheName(schema, nameObject.Name);
                    if (objectDics.ContainsKey(cacheKey))
                    {
                        objectDics[cacheKey].Add(nameObject);
                    }
                    else
                    {
                        objectDics[cacheKey] = new List<INameObject>();
                    }
                }
                string ObjectCacheName(string schema, string name) => $"{schema}___{name}";

            }

            void ApplyNamingRules()
            {
                var nameStyle = migrationSetting.TargetNameStyle;
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
                    table.Name = tableConvertFunc(table.Name);

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
        }



        
    }
}
