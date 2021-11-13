using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;


namespace ChangeDB.Agent.Postgres
{
    public class PostgresMetadataMigrator : IMetadataMigrator
    {
        public static readonly PostgresMetadataMigrator Default = new PostgresMetadataMigrator();
        public Task DropDatabaseIfExists(DbConnection connection, MigrationSetting migrationSetting)
        {
            connection.DropDatabaseIfExists();
            return Task.CompletedTask;
        }

        public Task CreateDatabase(DbConnection connection, MigrationSetting migrationSetting)
        {
            connection.CreateDatabase();
            return Task.CompletedTask;
        }

        public Task<DatabaseDescriptor> GetDatabaseDescriptor(DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            var databaseDescriptor = PostgresUtils.GetDataBaseDescriptorByEFCore(dbConnection);
            return Task.FromResult(databaseDescriptor);
        }
        public Task PreMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            CreateSchemas();
            CreateTables();
            CreatePrimaryKeys();
            CreateUniques();
            CreateIndexs();
            return Task.CompletedTask;

            void CreateSchemas()
            {
                foreach (var schema in databaseDescriptor.GetAllSchemas())
                {
                    dbConnection.ExecuteNonQuery($"CREATE SCHEMA IF NOT EXISTS {PostgresUtils.IdentityName(schema)};");
                }
            }
            void CreateTables()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    var columnDefines = string.Join(",", table.Columns.Select(p => $"{PostgresUtils.IdentityName(p.Name)} {p.StoreType}"));
                    dbConnection.ExecuteNonQuery($"CREATE TABLE {tableFullName} ({columnDefines});");
                }

            }
            void CreatePrimaryKeys()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    if (table.PrimaryKey == null) continue;
                    var primaryColumns = string.Join(",", table.PrimaryKey?.Columns.Select(p => PostgresUtils.IdentityName(p)));
                    if (string.IsNullOrEmpty(table.PrimaryKey.Name) && table.PrimaryKey.Columns?.Count > 0)
                    {
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD PRIMARY KEY ({primaryColumns})");
                    }
                    else
                    {
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD constraint {PostgresUtils.IdentityName(table.PrimaryKey.Name)} PRIMARY KEY ({primaryColumns})");
                    }
                }
            }
            void CreateUniques()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    foreach (var unique in table.Uniques)
                    {
                        var uniquename = PostgresUtils.IdentityName(unique.Name);
                        var uniqueColumns = string.Join(",", unique.Columns.Select(p => PostgresUtils.IdentityName(p)));
                        dbConnection.ExecuteNonQuery($"AlTER TABLE {tableFullName} ADD constraint {uniquename} unique ({uniqueColumns})");
                    }
                }
            }
            void CreateIndexs()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    foreach (var index in table.Indexes)
                    {
                        var indexName = PostgresUtils.IdentityName(index.Name);
                        var indexColumns = string.Join(",", index.Columns.Select(p => PostgresUtils.IdentityName(p)));
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
        }
        public Task PostMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            AlterNotnullColumns();
            AlterIdentityColumn();
            AddDefaultValue();
            AddForeignKeys();
            void AlterNotnullColumns()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    foreach (var column in table.Columns)
                    {
                        if (!column.IsNullable)
                        {
                            dbConnection.ExecuteNonQuery($"alter table {PostgresUtils.IdentityName(table.Schema, table.Name)} alter column {PostgresUtils.IdentityName(column.Name)} set not null;");
                        }
                    }
                }
            }
            void AlterIdentityColumn()
            {
                // TODO

            }
            void AddDefaultValue()
            {
                // 
            }
            void AddForeignKeys()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        var foreignKeyName = PostgresUtils.IdentityName(foreignKey.Name);
                        var foreignColumns = string.Join(",", foreignKey.ColumnNames.Select(p => PostgresUtils.IdentityName(p)));
                        var principalColumns = string.Join(",", foreignKey.PrincipalNames.Select(p => PostgresUtils.IdentityName(p)));
                        var principalTable = PostgresUtils.IdentityName(foreignKey.PrincipalSchema, foreignKey.PrincipalTable);
                        dbConnection.ExecuteNonQuery($"alter table {PostgresUtils.IdentityName(table.Schema, table.Name)} ADD CONSTRAINT {foreignKeyName}" +
                            $"FOREIGN KEY ({foreignColumns}) REFERENCES {principalTable}({principalColumns})");
                    }
                }
            }
            return Task.CompletedTask;
        }

    }
}
