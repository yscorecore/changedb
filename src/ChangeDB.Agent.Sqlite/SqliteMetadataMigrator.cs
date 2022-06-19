using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.Sqlite.SqliteUtils;

namespace ChangeDB.Agent.Sqlite
{
    public class SqliteMetadataMigrator : IMetadataMigrator
    {
        public static readonly IMetadataMigrator Default = new SqliteMetadataMigrator();

        public virtual Task<DatabaseDescriptor> GetDatabaseDescriptor(AgentContext agentContext)
        {
            var databaseDescriptor = GetDataBaseDescriptorByEFCore(agentContext.Connection);
            return Task.FromResult(databaseDescriptor);
        }

        public virtual Task PreMigrateMetadata(DatabaseDescriptor databaseDescriptor, AgentContext agentContext)
        {
            var dataTypeMapper = SqliteDataTypeMapper.Default;
            var sqlExpressionTranslator = SqliteSqlExpressionTranslator.Default;
            var dbConnection = agentContext.Connection;
            CreateTables();
            return Task.CompletedTask;
            void CreateTables()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Name);
                    var columnDefines = string.Join(", ", table.Columns.Select(p => $"{BuildColumnBasicDesc(p, table.PrimaryKey)}"));
                    var sql = @$"CREATE TABLE {tableFullName}
(
{columnDefines}
{BuildCompositePrimaryKeyDesc(table.PrimaryKey)}
{BuildUniqueDesc(table.Uniques)}
);";
                    agentContext.CreateTargetObject(sql, ObjectType.Table, tableFullName);
                }
                string BuildColumnBasicDesc(ColumnDescriptor column, PrimaryKeyDescriptor pk)
                {
                    var columnName = IdentityName(column.Name);
                    var dataType = dataTypeMapper.ToDatabaseStoreType(column.DataType);
                    var nullable = column.IsNullable ? string.Empty : "NOT NULL";
                    var isPrimaryKey = pk is not null && pk.Columns.Count == 1 && pk.Columns[0] == column.Name;
                    var primaryKey = isPrimaryKey ? "PRIMARY KEY" : string.Empty;
                    var increment = isPrimaryKey && column.IsIdentity && column.IdentityInfo != null ? "AUTOINCREMENT" : string.Empty;
                    var defaultValue = sqlExpressionTranslator.FromCommonSqlExpression(column.DefaultValue, dataType, column.DataType);
                    var defaultValueExpression = string.Empty;
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        var expression = defaultValue.StartsWith('(') && defaultValue.EndsWith(')') ? defaultValue : $"({defaultValue})";
                        defaultValueExpression = $"DEFAULT {expression}";
                    }
                    return $"{columnName} {dataType} {nullable} {primaryKey} {increment} {defaultValueExpression}".Trim();
                }
                string BuildCompositePrimaryKeyDesc(PrimaryKeyDescriptor pk)
                {
                    return pk is not null && pk.Columns.Count > 1
                        ? $", PRIMARY KEY({pk.Columns.Select(c => IdentityName(c))})"
                        : string.Empty;
                }
                string BuildUniqueDesc(List<UniqueDescriptor> uniques)
                {
                    return string.Join(Environment.NewLine, uniques.Select(u => $", UNIQUE({string.Join(",", u.Columns.Select(c => IdentityName(c)))})"));
                }
            }
        }

        public virtual Task PostMigrateMetadata(DatabaseDescriptor databaseDescriptor, AgentContext agentContext)
        {
            var dataTypeMapper = SqliteDataTypeMapper.Default;
            var sqlExpressionTranslator = SqliteSqlExpressionTranslator.Default;
            var dbConnection = agentContext.Connection;
            CreateIndexs();
            //AddForeignKeys();

            void CreateIndexs()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Name);
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
                        var principalTable = IdentityName(foreignKey.PrincipalTable);
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
