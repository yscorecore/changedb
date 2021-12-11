using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Default
{
    class SettingsApplier
    {

        public static void ApplySettingForTarget(AgentRunTimeInfo source, AgentRunTimeInfo target, MigrationSetting migrationSetting)
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

        public static Task ApplyAgentSettings(AgentRunTimeInfo target)
        {
            FixEmptySchema();
            FixMaxObjectName();
            return Task.CompletedTask;
            void FixMaxObjectName()
            {
                var agentSetting = target.Agent.AgentSetting;

                FixTableNameMaxLimit(agentSetting.ObjectNameMaxLength);
                FixColumnNameMaxLimit(agentSetting.ObjectNameMaxLength);
                FixIndexNameMaxLimit(agentSetting.ObjectNameMaxLength);
                FixUniqueNameMaxLimit(agentSetting.ObjectNameMaxLength);
                FixPrimaryKeyMaxLimit(agentSetting.ObjectNameMaxLength);
                FixForeignKeyMaxLimit(agentSetting.ObjectNameMaxLength);
                void FixTableNameMaxLimit(int objectMaxNameLength)
                {
                    var nameMap = target.Descriptor.Tables.Where(p => p?.Name?.Length > objectMaxNameLength)
                        .ToDictionary(p => $"{p.Schema}___{p.Name}", p => GetNewName(p.Name, objectMaxNameLength));

                    foreach (var table in target.Descriptor.Tables)
                    {
                        if (nameMap.ContainsKey($"{table.Schema}___{table.Name}"))
                        {
                            table.Name = nameMap[$"{table.Schema}___{table.Name}"];
                        }
                        table.ForeignKeys.Where(p => nameMap.ContainsKey($"{p.PrincipalSchema}___{p.PrincipalTable}"))
                            .Each(p => p.Name = nameMap[$"{p.PrincipalSchema}___{p.PrincipalTable}"]);
                    }

                }

                void FixColumnNameMaxLimit(int objectMaxNameLength)
                {
                    //TODO
                }

                void FixIndexNameMaxLimit(int objectMaxNameLength)
                {
                    target.Descriptor.Tables.SelectMany(p => p.Indexes)
                         .Where(p => p.Name?.Length > objectMaxNameLength)
                         .Each(p => p.Name = GetNewName(p.Name, objectMaxNameLength));
                }

                void FixUniqueNameMaxLimit(int objectMaxNameLength)
                {
                    target.Descriptor.Tables.SelectMany(p => p.Uniques)
                        .Where(p => p?.Name?.Length > objectMaxNameLength)
                        .Each(p => p.Name = GetNewName(p.Name, objectMaxNameLength));
                }

                void FixPrimaryKeyMaxLimit(int objectMaxNameLength)
                {
                    target.Descriptor.Tables.Select(p => p.PrimaryKey)
                        .Where(p => p?.Name?.Length > objectMaxNameLength)
                        .Each(p => p.Name = GetNewName(p.Name, objectMaxNameLength));
                }

                void FixForeignKeyMaxLimit(int objectMaxNameLength)
                {
                    target.Descriptor.Tables.SelectMany(p => p.ForeignKeys)
                        .Where(p => p?.Name?.Length > objectMaxNameLength)
                        .Each(p => p.Name = GetNewName(p.Name, objectMaxNameLength));
                }
            }
            void FixEmptySchema()
            {
                var agentSetting = target.Agent.AgentSetting;
                if (string.IsNullOrEmpty(agentSetting.DefaultSchema))
                {
                    // clear schemas
                    target.Descriptor.Tables.Each(p => p.Schema = null);
                    target.Descriptor.Tables.SelectMany(p => p.ForeignKeys).Each(p => p.PrincipalSchema = null);
                    target.Descriptor.Sequences.Each(p => p.Schema = null);
                }
                else
                {
                    // set defaultSchame
                    target.Descriptor.Tables.Where(p => string.IsNullOrEmpty(p.Schema)).Each(p => p.Schema = agentSetting.DefaultSchema);
                    target.Descriptor.Tables.SelectMany(p => p.ForeignKeys).Where(p => string.IsNullOrEmpty(p.PrincipalSchema)).Each(p => p.PrincipalSchema = agentSetting.DefaultSchema);
                    target.Descriptor.Sequences.Where(p => string.IsNullOrEmpty(p.Schema)).Each(p => p.Schema = agentSetting.DefaultSchema);
                }
            }

        }
        private static string GetNewName(string originName, int maxLength) => $"{originName.Substring(0, maxLength - 9)}_{originName.FixedHash():X8}";

    }
}
