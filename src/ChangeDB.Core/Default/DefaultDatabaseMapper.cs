using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Mapper;
using ChangeDB.Migration;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Default
{
    public class DefaultDatabaseMapper : IDatabaseMapper
    {
        public Task<DatabaseDescriptorMapper> MapDatabase(DatabaseDescriptor sourceDatabase, AgentSetting agentSetting, MigrationSetting migrationSetting)
        {
            var mapper = CreateDefaultMapper(sourceDatabase);

            RemoveBrokenForeignKeys(mapper.Target);
            FixDuplicateObjectName(mapper.Target);
            ApplyNamingRules(mapper.Target, migrationSetting);
            FixEmptySchema(agentSetting, migrationSetting, mapper.Target);
            FixMaxObjectName(agentSetting, mapper.Target);
            FixSqlServerSchemaNameIssue(mapper.Target);
            return Task.FromResult(mapper);
        }

        private DatabaseDescriptorMapper CreateDefaultMapper(DatabaseDescriptor sourceDatabase)
        {
            var databaseMapper = new DatabaseDescriptorMapper { Source = sourceDatabase, Target = new DatabaseDescriptor() };
            // sequence
            foreach (var seq in sourceDatabase.Sequences)
            {
                var seqMapper = new SequenceDescriptorMapper { Source = seq, Target = seq.DeepClone() };
                databaseMapper.SequenceMappers.Add(seqMapper);
                databaseMapper.Target.Sequences.Add(seqMapper.Target);
            }
            // table
            foreach (var table in sourceDatabase.Tables)
            {
                var tableMapper = new TableDescriptorMapper
                {
                    Source = table,
                    Target = new TableDescriptor { Name = table.Name, Comment = table.Comment, Schema = table.Schema }
                };
                foreach (var column in table.Columns)
                {
                    var columnMapper = new ColumnDescriptorMapper { Source = column, Target = column.DeepClone() };
                    tableMapper.ColumnMappers.Add(columnMapper);
                    tableMapper.Target.Columns.Add(columnMapper.Target);
                }

                databaseMapper.TableMappers.Add(tableMapper);
                databaseMapper.Target.Tables.Add(tableMapper.Target);
            }

            return databaseMapper;
        }

        void RemoveBrokenForeignKeys(DatabaseDescriptor targetDatabase)
        {
            // when custom filter some objects, maybe happen
            var allTables = targetDatabase.Tables.Select(p => Key(p.Schema, p.Name)).ToHashSet();
            targetDatabase.Tables.ForEach((t) =>
            {
                if (t.ForeignKeys == null)
                {
                    return;
                }

                var brokenForeignKeys = t.ForeignKeys
                    .Where(f => !allTables.Contains(Key(f.PrincipalSchema, f.PrincipalTable))).ToList();
                brokenForeignKeys.ForEach(f => t.ForeignKeys.Remove(f));
            });
            string Key(string schema, string name) => $"\"{schema}\".\"{name}\"";
        }
        void FixDuplicateObjectName(DatabaseDescriptor targetDatabase)
        {

            var objectDics = targetDatabase.Tables.ToDictionary(p => ObjectCacheName(p.Schema, p.Name), p => new List<INameObject>());
            foreach (var table in targetDatabase.Tables)
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

        void ApplyNamingRules(DatabaseDescriptor targetDatabase, MigrationSetting migrationSetting)
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
            foreach (var table in targetDatabase.Tables)
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
            foreach (var sequence in targetDatabase.Sequences)
            {
                sequence.Schema = schemaConvertFunc(sequence.Schema);
                sequence.Name = sequenceConvertFunc(sequence.Name);
            }

        }

        void FixMaxObjectName(AgentSetting agentSetting, DatabaseDescriptor targetDatabase)
        {

            FixTableNameMaxLimit(agentSetting.ObjectNameMaxLength);
            FixColumnNameMaxLimit(agentSetting.ObjectNameMaxLength);
            FixIndexNameMaxLimit(agentSetting.ObjectNameMaxLength);
            FixUniqueNameMaxLimit(agentSetting.ObjectNameMaxLength);
            FixPrimaryKeyMaxLimit(agentSetting.ObjectNameMaxLength);
            FixForeignKeyMaxLimit(agentSetting.ObjectNameMaxLength);
            void FixTableNameMaxLimit(int objectMaxNameLength)
            {
                var nameMap = targetDatabase.Tables.Where(p => p?.Name?.Length > objectMaxNameLength)
                    .ToDictionary(p => $"{p.Schema}___{p.Name}", p => GetNewName(p.Name, objectMaxNameLength));

                foreach (var table in targetDatabase.Tables)
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
                targetDatabase.Tables.SelectMany(p => p.Indexes)
                     .Where(p => p.Name?.Length > objectMaxNameLength)
                     .Each(p => p.Name = GetNewName(p.Name, objectMaxNameLength));
            }

            void FixUniqueNameMaxLimit(int objectMaxNameLength)
            {
                targetDatabase.Tables.SelectMany(p => p.Uniques)
                    .Where(p => p?.Name?.Length > objectMaxNameLength)
                    .Each(p => p.Name = GetNewName(p.Name, objectMaxNameLength));
            }

            void FixPrimaryKeyMaxLimit(int objectMaxNameLength)
            {
                targetDatabase.Tables.Select(p => p.PrimaryKey)
                    .Where(p => p?.Name?.Length > objectMaxNameLength)
                    .Each(p => p.Name = GetNewName(p.Name, objectMaxNameLength));
            }

            void FixForeignKeyMaxLimit(int objectMaxNameLength)
            {
                targetDatabase.Tables.SelectMany(p => p.ForeignKeys)
                    .Where(p => p?.Name?.Length > objectMaxNameLength)
                    .Each(p => p.Name = GetNewName(p.Name, objectMaxNameLength));
            }
        }

        void FixEmptySchema(AgentSetting agentSetting, MigrationSetting migrationSetting, DatabaseDescriptor targetDatabase)
        {
            if (string.IsNullOrEmpty(agentSetting.DefaultSchema))
            {
                // clear schemas
                targetDatabase.Tables.Each(p => p.Schema = null);
                targetDatabase.Tables.SelectMany(p => p.ForeignKeys).Each(p => p.PrincipalSchema = null);
                targetDatabase.Sequences.Each(p => p.Schema = null);
            }
            else
            {
                var targetDefaultSchema = string.IsNullOrEmpty(migrationSetting.TargetDefaultSchema)
                    ? agentSetting.DefaultSchema
                    : migrationSetting.TargetDefaultSchema;
                // set defaultSchame
                targetDatabase.Tables.Where(p => string.IsNullOrEmpty(p.Schema)).Each(p => p.Schema = targetDefaultSchema);
                targetDatabase.Tables.SelectMany(p => p.ForeignKeys).Where(p => string.IsNullOrEmpty(p.PrincipalSchema)).Each(p => p.PrincipalSchema = targetDefaultSchema);
                targetDatabase.Sequences.Where(p => string.IsNullOrEmpty(p.Schema)).Each(p => p.Schema = targetDefaultSchema);
            }
        }
        private static string GetNewName(string originName, int maxLength) => $"{originName.Substring(0, maxLength - 9)}_{originName.FixedHash():X8}";

        void FixSqlServerSchemaNameIssue(DatabaseDescriptor targetDatabase, AgentSetting agentSetting)
        {
            // sqlserver cannot support schema named "public"
            if ("sqlserver".Equals(agentSetting.DatabaseType, StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var table in targetDatabase.Tables)
                { 
                
                }
                foreach (var seq in targetDatabase.Sequences)
                {
                    if ("public".Equals(seq.Schema, StringComparison.InvariantCultureIgnoreCase))
                    {
                        seq.Schema = $"{seq.Schema}_";
                    }
                }
            }

            
        }
    }
}
