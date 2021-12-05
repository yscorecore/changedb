using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerMetadataMigrator : IMetadataMigrator
    {
        public static readonly IMetadataMigrator Default = new SqlServerMetadataMigrator();

        public virtual Task<DatabaseDescriptor> GetDatabaseDescriptor(DbConnection connection, MigrationSetting migrationSetting)
        {
            var databaseDescriptor = SqlServerUtils.GetDataBaseDescriptorByEFCore(connection);
            return Task.FromResult(databaseDescriptor);
        }

        public virtual Task PreMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            CreateSchemas();
            CreateTables();
            CreatePrimaryKeys();
            return Task.CompletedTask;

            void CreateSchemas()
            {
                foreach (var schema in databaseDescriptor.GetAllSchemas())
                {
                    dbConnection.ExecuteNonQuery($"IF NOT EXISTS (SELECT  * FROM SYS.SCHEMAS WHERE NAME = N'{schema}') EXEC('CREATE SCHEMA {IdentityName(schema)}')");
                }
            }
            void CreateTables()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    var columnDefines = string.Join(", ", table.Columns.Select(p => $"{BuildColumnBasicDesc(p)}"));
                    dbConnection.ExecuteNonQuery($"CREATE TABLE {tableFullName} ({columnDefines});");
                }
                string BuildColumnBasicDesc(ColumnDescriptor column)
                {
                    var columnName = IdentityName(column.Name);
                    var dataType = column.StoreType;
                    var identityInfo = column.IsIdentity && column.IdentityInfo != null
                        ? $"IDENTITY({column.IdentityInfo.StartValue},{column.IdentityInfo.IncrementBy})"
                        : string.Empty;
                    return $"{columnName} {dataType} {identityInfo}".TrimEnd();
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
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {columnName} {columnDesc.StoreType} NOT NULL;");
                        if (!string.IsNullOrEmpty(columnDesc.DefaultValueSql))
                        {
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD CONSTRAINT DF_DEFAULT_{table.Schema}_{table.Name}_{column} DEFAULT ({columnDesc.DefaultValueSql}) FOR {columnName};");
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

        public virtual Task PostMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            AlterNotNullColumns();
            AddDefaultValues();
            CreateUniques();
            CreateIndexs();
            AddForeignKeys();
            void AlterNotNullColumns()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    foreach (var column in table.Columns)
                    {
                        var columnName = IdentityName(column.Name);
                        var isPrimaryKey = table.PrimaryKey?.Columns?.Contains(column.Name) ?? false;

                        if (column.IdentityInfo == null && !column.IsNullable && !isPrimaryKey)
                        {
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {columnName} {column.StoreType} NOT NULL");
                        }
                    }
                }
            }

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
                            dbConnection.ExecuteNonQuery($"CREATE UNIQUE INDEX {indexName} ON {tableFullName}({indexColumns});");
                        }
                        else
                        {
                            dbConnection.ExecuteNonQuery($"CREATE INDEX {indexName} ON {tableFullName}({indexColumns});");
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
                        var isPrimaryKey = table.PrimaryKey?.Columns?.Contains(column.Name) ?? false;
                        if (!string.IsNullOrEmpty(column.DefaultValueSql) && !isPrimaryKey)
                        {
                            var columnName = IdentityName(column.Name);
                            var constraintName = Regex.Replace($"DF_{table.Name}_{column.Name}", "[^a-zA-Z1-9]", "_");
                            var defaultValueExpression = column.DefaultValueSql.StartsWith('(') && column.DefaultValueSql.EndsWith(')') ? column.DefaultValueSql : $"({column.DefaultValueSql})";
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD CONSTRAINT {constraintName} DEFAULT {defaultValueExpression} FOR {columnName};");
                        }
                    }
                }
            }
            void AddForeignKeys()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = IdentityName(table.Schema, table.Name);
                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        var foreignKeyName = IdentityName(foreignKey.Name);
                        var foreignColumns = string.Join(",", foreignKey.ColumnNames.Select(p => IdentityName(p)));
                        var principalColumns = string.Join(",", foreignKey.PrincipalNames.Select(p => IdentityName(p)));
                        var principalTable = IdentityName(foreignKey.PrincipalSchema, foreignKey.PrincipalTable);
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {IdentityName(table.Schema, table.Name)} ADD CONSTRAINT {foreignKeyName}" +
                            $" FOREIGN KEY ({foreignColumns}) REFERENCES {principalTable}({principalColumns})");
                    }
                }
            }
            return Task.CompletedTask;
        }

        protected virtual string IdentityName(string schema, string objectName)
        {
            return SqlServerUtils.IdentityName(schema, objectName);
        }
        protected virtual string IdentityName(string objectName)
        {
            return SqlServerUtils.IdentityName(objectName);
        }
    }
}
