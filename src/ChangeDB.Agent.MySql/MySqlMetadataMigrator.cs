using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.MySql.MySqlUtils;
namespace ChangeDB.Agent.MySql
{
    public class MySqlMetadataMigrator : IMetadataMigrator
    {

        public static readonly IMetadataMigrator Default = new MySqlMetadataMigrator();

        public Task<DatabaseDescriptor> GetSourceDatabaseDescriptor(MigrationContext migrationContext)
        {
            // mysql get descriptor need a new connection
            using var newSourceConnection =
                migrationContext.Source.Agent.CreateConnection(migrationContext.SourceDatabase.ConnectionString);
            var databaseDescriptor = GetDataBaseDescriptorByEFCore(newSourceConnection, MySqlDataTypeMapper.Default, MySqlExpressionTranslator.Default);
            return Task.FromResult(databaseDescriptor);
        }

        public Task PreMigrateTargetMetadata(DatabaseDescriptor databaseDescriptor, MigrationContext migrationContext)
        {
            var datatypeMapper = MySqlDataTypeMapper.Default;
            var sqlExpressionTranslator = MySqlExpressionTranslator.Default;
            CreateTables();
            CreateIndexs();
            return Task.CompletedTask;

            void CreateTables()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    var lines = table.Columns.Select(p => $"{BuildColumnBasicDesc(p)}").ToList();
                    AppendPrimaryKeyLine(table, lines);
                    AppendUniqueConstraintLines(table, lines);
                    var columnDefines = string.Join(", ", lines);
                    var sql = $"CREATE TABLE {tableFullName} ({columnDefines});";
                    migrationContext.CreateTargetObject(sql, ObjectType.Table, tableFullName);
                }
                string BuildColumnBasicDesc(ColumnDescriptor column)
                {
                    var columnName = IdentityName(column.Name);
                    var dataType = datatypeMapper.ToDatabaseStoreType(column.DataType);
                    var nullable = column.IsNullable ? "null" : "not null";

                    if (column.IsIdentity && column.IdentityInfo != null)
                    {
                        return $"{columnName} {dataType} {nullable} auto_increment";
                    }

                    else
                    {
                        return $"{columnName} {dataType} {nullable}";
                    }
                }

            }

            void AppendPrimaryKeyLine(TableDescriptor table, List<string> lines)
            {
                if (table.PrimaryKey?.Columns?.Count > 0)
                {
                    var primaryColumns = string.Join(",", table.PrimaryKey.Columns.Select(IdentityName));
                    lines.Add(string.IsNullOrEmpty(table.PrimaryKey.Name)
                        ? $"PRIMARY KEY ({primaryColumns})"
                        : $"CONSTRAINT {IdentityName(table.PrimaryKey.Name)} PRIMARY KEY ({primaryColumns})");
                }
            }

            void AppendUniqueConstraintLines(TableDescriptor table, List<string> lines)
            {
                table.Uniques.Each(u =>
                {
                    var columns = string.Join(",", u.Columns.Select(IdentityName));
                    lines.Add(string.IsNullOrEmpty(u.Name)
                        ? $"UNIQUE ({columns})"
                        : $"CONSTRAINT {IdentityName(u.Name)} UNIQUE ({columns})");
                });
            }

            void CreateIndexs()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    foreach (var index in table.Indexes.Where(p => !p.IsUnique))
                    {
                        var indexName = IdentityName(index.Name);
                        var indexColumns = string.Join(",", index.Columns.Select(p => IdentityName(p)));
                        var sql = $"CREATE INDEX {indexName} ON {tableFullName}({indexColumns})";
                        migrationContext.CreateTargetObject(sql, ObjectType.Index, indexName, tableFullName);
                    }
                }
            }
        }
        public Task PostMigrateTargetMetadata(DatabaseDescriptor databaseDescriptor, MigrationContext migrationContext)
        {
            var datatypeMapper = MySqlDataTypeMapper.Default;
            var sqlExpressionTranslator = MySqlExpressionTranslator.Default;
            var dbConnection = migrationContext.TargetConnection;
            AddDefaultValues();
            AddForeignKeys();

            void AddDefaultValues()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    foreach (var column in table.Columns)
                    {
                        var dataType = datatypeMapper.ToDatabaseStoreType(column.DataType);
                        var defaultValue =
                            sqlExpressionTranslator.FromCommonSqlExpression(column.DefaultValue, dataType);
                        if (!string.IsNullOrEmpty(defaultValue))
                        {
                            var columnName = IdentityName(column.Name);
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {columnName} SET DEFAULT {defaultValue};");
                        }
                    }
                }
            }
            void AddForeignKeys()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableName = IdentityName(table);
                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        var foreignKeyName = IdentityName(foreignKey.Name);
                        var foreignColumns = string.Join(", ", foreignKey.ColumnNames.Select(IdentityName));
                        var principalColumns = string.Join(", ", foreignKey.PrincipalNames.Select(IdentityName));
                        var principalTable = IdentityName(foreignKey.PrincipalSchema, foreignKey.PrincipalTable);
                        var sql =
                            $"ALTER TABLE {tableName} ADD CONSTRAINT {foreignKeyName}" +
                            $"FOREIGN KEY ({foreignColumns}) REFERENCES {principalTable}({principalColumns})";
                        migrationContext.CreateTargetObject(sql, ObjectType.ForeignKey, foreignKeyName, tableName);
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
