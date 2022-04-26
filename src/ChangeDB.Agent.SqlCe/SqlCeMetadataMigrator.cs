using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChangeDB.Migration;

using static ChangeDB.Agent.SqlCe.SqlCeUtils;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeMetadataMigrator : IMetadataMigrator
    {
        public static readonly IMetadataMigrator Default = new SqlCeMetadataMigrator();

        [System.Obsolete]
        public virtual Task<DatabaseDescriptor> GetDatabaseDescriptor(AgentContext agentContext)
        {
            var databaseDescriptor = SqlCeUtils.GetDataBaseDescriptorByEFCore(agentContext.Connection);
            return Task.FromResult(databaseDescriptor);
        }

        public virtual Task PreMigrateMetadata(DatabaseDescriptor databaseDescriptor, AgentContext agentContext)
        {
            var dataTypeMapper = SqlCeDataTypeMapper.Default;
            var sqlExpressionTranslator = SqlCeSqlExpressionTranslator.Default;
            var dbConnection = agentContext.Connection;
            CreateTables();
            CreatePrimaryKeys();
            return Task.CompletedTask;

            void CreateTables()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    var columnDefines = string.Join(", ", table.Columns.Select(p => $"{BuildColumnBasicDesc(p)}"));
                    var sql = $"CREATE TABLE {tableFullName} ({columnDefines});";
                    agentContext.CreateTargetObject(sql, ObjectType.Table, tableFullName);
                }
                string BuildColumnBasicDesc(ColumnDescriptor column)
                {
                    var columnName = IdentityName(column.Name);
                    var dataType = dataTypeMapper.ToDatabaseStoreType(column.DataType);
                    var nullable = column.IsNullable ? string.Empty : "NOT NULL";
                    var identityInfo = column.IsIdentity && column.IdentityInfo != null
                        ? $"IDENTITY({column.IdentityInfo.StartValue},{column.IdentityInfo.IncrementBy})"
                        : string.Empty;
                    return $"{columnName} {dataType} {nullable} {identityInfo}".TrimEnd();
                }
            }
            void CreatePrimaryKeys()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    if (table.PrimaryKey == null) continue;
                    var primaryColumns = string.Join(", ", table.PrimaryKey?.Columns.Select(p => IdentityName(p)));

                    // set primary key columns NOT NULL and with default value
                    foreach (var column in table.PrimaryKey?.Columns ?? Enumerable.Empty<string>())
                    {
                        var columnName = IdentityName(column);
                        var columnDesc = table.Columns.Single(p => p.Name == column);
                        var dataType = dataTypeMapper.ToDatabaseStoreType(columnDesc.DataType);
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {columnName} {dataType} NOT NULL;");
                        if (columnDesc.DefaultValue != null)
                        {
                            var defaultValue =
                            sqlExpressionTranslator.FromCommonSqlExpression(columnDesc.DefaultValue, dataType);
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD CONSTRAINT DF_DEFAULT_{table.Schema}_{table.Name}_{column} DEFAULT ({defaultValue}) FOR {columnName};");
                        }
                    }

                    if (string.IsNullOrEmpty(table.PrimaryKey.Name) && table.PrimaryKey.Columns?.Count > 0)
                    {
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD PRIMARY KEY ({primaryColumns})");
                    }
                    else
                    {
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD CONSTRAINT {IdentityName(table.PrimaryKey.Name)} PRIMARY KEY ({primaryColumns})");
                    }
                }
            }

        }

        public virtual Task PostMigrateMetadata(DatabaseDescriptor databaseDescriptor, AgentContext agentContext)
        {
            var dataTypeMapper = SqlCeDataTypeMapper.Default;
            var sqlExpressionTranslator = SqlCeSqlExpressionTranslator.Default;
            var dbConnection = agentContext.Connection;

            AddDefaultValues();
            CreateUniques();
            CreateIndexs();
            AddForeignKeys();


            void CreateUniques()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    foreach (var unique in table.Uniques)
                    {
                        var uniquename = IdentityName(unique.Name);
                        var uniqueColumns = string.Join(",", unique.Columns.Select(p => IdentityName(p)));
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD CONSTRAINT{uniquename} unique ({uniqueColumns})");
                    }
                }
            }
            void CreateIndexs()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    foreach (var index in table.Indexes)
                    {
                        var indexName = IdentityName(index.Name);
                        var indexColumns = string.Join(",", index.Columns.Select(p => IdentityName(p)));
                        if (index.IsUnique)
                        {
                            var sql = $"CREATE UNIQUE INDEX {indexName} ON {tableFullName}({indexColumns});";
                            agentContext.CreateTargetObject(sql, ObjectType.UniqueIndex, indexName, tableFullName);
                        }
                        else
                        {
                            var sql = $"CREATE INDEX {indexName} ON {tableFullName}({indexColumns});";
                            agentContext.CreateTargetObject(sql, ObjectType.Index, indexName, tableFullName);
                        }
                    }
                }
            }

            void AddDefaultValues()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    foreach (var column in table.Columns)
                    {
                        if (column.IsIdentity || column.DefaultValue == null) continue;
                        var isPrimaryKey = table.PrimaryKey?.Columns?.Contains(column.Name) ?? false;
                        var dataType = dataTypeMapper.ToDatabaseStoreType(column.DataType);
                        var defaultValue =
                            sqlExpressionTranslator.FromCommonSqlExpression(column.DefaultValue, dataType);
                        if (!column.IsIdentity && !string.IsNullOrEmpty(defaultValue) && !isPrimaryKey)
                        {
                            var columnName = IdentityName(column.Name);
                            var constraintName = Regex.Replace($"DF_{table.Name}_{column.Name}", "[^a-zA-Z1-9]", "_");
                            var defaultValueExpression = defaultValue.StartsWith('(') && defaultValue.EndsWith(')') ? defaultValue : $"({defaultValue})";
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD CONSTRAINT {constraintName} DEFAULT {defaultValueExpression} FOR {columnName};");
                        }
                    }
                }
            }
            void AddForeignKeys()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table);
                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        var foreignKeyName = IdentityName(foreignKey.Name);
                        var foreignColumns = string.Join(",", foreignKey.ColumnNames.Select(IdentityName));
                        var principalColumns = string.Join(",", foreignKey.PrincipalNames.Select(p => IdentityName(p)));
                        var principalTable = IdentityName(foreignKey.PrincipalSchema, foreignKey.PrincipalTable);
                        var sql =
                            $"ALTER TABLE {tableFullName} ADD CONSTRAINT {foreignKeyName}" +
                            $" FOREIGN KEY ({foreignColumns}) REFERENCES {principalTable}({principalColumns})";
                        agentContext.CreateTargetObject(sql, ObjectType.ForeignKey, foreignKeyName, tableFullName);

                    }
                }
            }
            return Task.CompletedTask;
        }


    }
}
