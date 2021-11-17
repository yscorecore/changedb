using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerMetadataMigrator : IMetadataMigrator
    {
        public static readonly IMetadataMigrator Default = new SqlServerMetadataMigrator();

        public Task<DatabaseDescriptor> GetDatabaseDescriptor(DbConnection connection, MigrationSetting migrationSetting)
        {
            var databaseDescriptor = SqlServerUtils.GetDataBaseDescriptorByEFCore(connection);
            return Task.FromResult(databaseDescriptor);
        }

        public Task PreMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            CreateSchemas();
            CreateTables();
            CreatePrimaryKeys();
            return Task.CompletedTask;

            void CreateSchemas()
            {
                foreach (var schema in databaseDescriptor.GetAllSchemas())
                {
                    dbConnection.ExecuteNonQuery($"IF NOT EXISTS (SELECT  * FROM sys.schemas WHERE name = N'{schema}') EXEC('CREATE SCHEMA {SqlServerUtils.IdentityName(schema)}')");
                }
            }
            void CreateTables()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = SqlServerUtils.IdentityName(table.Schema, table.Name);
                    var columnDefines = string.Join(",", table.Columns.Select(p => $"{BuildColumnBasicDesc(p)}"));
                    dbConnection.ExecuteNonQuery($"CREATE TABLE {tableFullName} ({columnDefines});");
                }
                string BuildColumnBasicDesc(ColumnDescriptor column)
                {
                    var columnName = SqlServerUtils.IdentityName(column.Name);
                    var dataType = column.StoreType;
                    var identityInfo = column.IsIdentity && column.IdentityInfo != null
                        ? $"identity({column.IdentityInfo.StartValue},{column.IdentityInfo.IncrementBy})"
                        : string.Empty;
                    return $"{columnName} {dataType} {identityInfo}";
                }
            }
            void CreatePrimaryKeys()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = SqlServerUtils.IdentityName(table.Schema, table.Name);
                    if (table.PrimaryKey == null) continue;
                    var primaryColumns = string.Join(",", table.PrimaryKey?.Columns.Select(p => SqlServerUtils.IdentityName(p)));

                    // set primary key columns not null and with default value
                    foreach (var column in table.PrimaryKey?.Columns ?? Enumerable.Empty<string>())
                    {
                        var columnName = SqlServerUtils.IdentityName(column);
                        var columnDesc = table.Columns.Single(p => p.Name == column);
                        dbConnection.ExecuteNonQuery($"alter table {tableFullName} alter column {columnName} {columnDesc.StoreType} not null;");
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
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD constraint {SqlServerUtils.IdentityName(table.PrimaryKey.Name)} PRIMARY KEY ({primaryColumns})");
                    }
                }
            }

        }

        public Task PostMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
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
                    var tableFullName = SqlServerUtils.IdentityName(table.Schema, table.Name);
                    foreach (var column in table.Columns)
                    {
                        var columnName = SqlServerUtils.IdentityName(column.Name);
                        var isPrimaryKey = table.PrimaryKey?.Columns?.Contains(column.Name) ?? false;
                        if (!column.IsNullable && !isPrimaryKey)
                        {
                            dbConnection.ExecuteNonQuery($"alter table {tableFullName} alter column {columnName} {column.StoreType} not null");
                        }
                    }
                }
            }

            void CreateUniques()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = SqlServerUtils.IdentityName(table.Schema, table.Name);
                    foreach (var unique in table.Uniques)
                    {
                        var uniquename = SqlServerUtils.IdentityName(unique.Name);
                        var uniqueColumns = string.Join(",", unique.Columns.Select(p => SqlServerUtils.IdentityName(p)));
                        dbConnection.ExecuteNonQuery($"AlTER TABLE {tableFullName} ADD constraint {uniquename} unique ({uniqueColumns})");
                    }
                }
            }
            void CreateIndexs()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = SqlServerUtils.IdentityName(table.Schema, table.Name);
                    foreach (var index in table.Indexes)
                    {
                        var indexName = SqlServerUtils.IdentityName(index.Name);
                        var indexColumns = string.Join(",", index.Columns.Select(p => SqlServerUtils.IdentityName(p)));
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
                    var tableFullName = SqlServerUtils.IdentityName(table.Schema, table.Name);
                    foreach (var column in table.Columns)
                    {
                        var isPrimaryKey = table.PrimaryKey?.Columns?.Contains(column.Name) ?? false;
                        if (!string.IsNullOrEmpty(column.DefaultValueSql) && !isPrimaryKey)
                        {
                            var columnName = SqlServerUtils.IdentityName(column.Name);
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD CONSTRAINT DF_DEFAULT_{table.Name}_{column.Name} DEFAULT ({column.DefaultValueSql}) FOR {columnName};");
                        }
                    }
                }
            }
            void AddForeignKeys()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = SqlServerUtils.IdentityName(table.Schema, table.Name);
                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        var foreignKeyName = SqlServerUtils.IdentityName(foreignKey.Name);
                        var foreignColumns = string.Join(",", foreignKey.ColumnNames.Select(p => SqlServerUtils.IdentityName(p)));
                        var principalColumns = string.Join(",", foreignKey.PrincipalNames.Select(p => SqlServerUtils.IdentityName(p)));
                        var principalTable = SqlServerUtils.IdentityName(foreignKey.PrincipalSchema, foreignKey.PrincipalTable);
                        dbConnection.ExecuteNonQuery($"alter table {SqlServerUtils.IdentityName(table.Schema, table.Name)} ADD CONSTRAINT {foreignKeyName}" +
                            $"FOREIGN KEY ({foreignColumns}) REFERENCES {principalTable}({principalColumns})");
                    }
                }
            }
            return Task.CompletedTask;
        }

    }
}
