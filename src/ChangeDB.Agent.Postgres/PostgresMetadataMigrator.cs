﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.Postgres.PostgresUtils;



namespace ChangeDB.Agent.Postgres
{
    public class PostgresMetadataMigrator : IMetadataMigrator
    {

        public static readonly PostgresMetadataMigrator Default = new PostgresMetadataMigrator();

        public Task<DatabaseDescriptor> GetSourceDatabaseDescriptor(MigrationContext migrationContext)
        {
            var databaseDescriptor = PostgresUtils.GetDataBaseDescriptorByEfCore(migrationContext.SourceConnection);
            return Task.FromResult(databaseDescriptor);
        }
        public Task PreMigrateTargetMetadata(DatabaseDescriptor databaseDescriptor, MigrationContext migrationContext)
        {
            var dbConnection = migrationContext.TargetConnection;
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
                    var schemaName = PostgresUtils.IdentityName(schema);
                    var sql = $"CREATE SCHEMA IF NOT EXISTS {schemaName}";
                    migrationContext.CreateTargetObject(sql, ObjectType.Schema, schemaName);
                }
            }
            void CreateTables()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    var columnDefines = string.Join(", ", table.Columns.Select(p => $"{BuildColumnBasicDesc(p)}"));
                    var sql = $"CREATE TABLE {tableFullName} ({columnDefines});";
                    migrationContext.CreateTargetObject(sql, ObjectType.Table, tableFullName);
                }
                string BuildColumnBasicDesc(ColumnDescriptor column)
                {
                    var columnName = PostgresUtils.IdentityName(column.Name);
                    var dataType = column.StoreType;

                    if (column.IsIdentity && column.IdentityInfo != null)
                    {
                        var identityInfo = column.IdentityInfo;
                        var identityType = PostgresUtils.IdentityAlways;

                        if (identityInfo.Values != null && identityInfo.Values.TryGetValue(PostgresUtils.IdentityType, out var type))
                        {
                            identityType = Convert.ToString(type);
                        }

                        var identityDetails = $"GENERATED {identityType} AS IDENTITY {BuildIdentityDetails(column.IdentityInfo)}";
                        return $"{columnName} {dataType} {identityDetails}";
                    }
                    else if (column.IdentityInfo != null)
                    {
                        //serial
                        var mappedDataType = column.StoreType.ToLowerInvariant() switch
                        {
                            "integer" => "serial",
                            "smallint" => "smallserial",
                            "bigint" => "bigserial",
                            _ => throw new ArgumentException($"not support {column.StoreType} as serial"),
                        };
                        return $"{columnName} {mappedDataType}";
                    }
                    else
                    {
                        return $"{columnName} {dataType}";
                    }
                }
                // https://www.postgresql.org/docs/current/sql-altersequence.html
                string BuildIdentityDetails(IdentityDescriptor desc)
                {
                    var items = new List<string>();

                    items.Add($"START WITH {desc.StartValue}");
                    items.Add($"INCREMENT BY {desc.IncrementBy}");

                    if (desc.MinValue != null)
                    {
                        items.Add($"MINVALUE {desc.MinValue}");
                    }
                    else if (desc.StartValue < 1 && desc.IncrementBy > 0)
                    {
                        // the minvalue default is 1
                        items.Add($"MINVALUE {desc.StartValue}");
                    }

                    if (desc.MaxValue != null)
                    {
                        items.Add($"MAXVALUE {desc.MaxValue}");
                    }
                    if (desc.IsCyclic == true)
                    {
                        items.Add("CYCLE");
                    }
                    if (desc.Values.TryGetValue(PostgresUtils.IdentityNumbersToCache, out var cache))
                    {
                        items.Add($"CACHE {cache}");
                    }
                    return $"({string.Join(" ", items)})";
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
                        dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ADD CONSTRAINT {PostgresUtils.IdentityName(table.PrimaryKey.Name)} PRIMARY KEY ({primaryColumns})");
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
                        var sql = $"ALTER TABLE {tableFullName} ADD CONSTRAINT {uniquename} unique ({uniqueColumns})";
                        migrationContext.CreateTargetObject(sql, ObjectType.Unique, uniquename, tableFullName);
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
                            var sql = $"CREATE UNIQUE INDEX {indexName} ON {tableFullName}({indexColumns})";
                            migrationContext.CreateTargetObject(sql, ObjectType.UniqueIndex, indexName, tableFullName);
                        }
                        else
                        {
                            var sql = $"CREATE INDEX {indexName} ON {tableFullName}({indexColumns})";
                            migrationContext.CreateTargetObject(sql, ObjectType.Index, indexName, tableFullName);
                        }
                    }
                }
            }
        }
        public Task PostMigrateTargetMetadata(DatabaseDescriptor databaseDescriptor, MigrationContext migrationContext)
        {
            var dbConnection = migrationContext.TargetConnection;
            AlterNotnullColumns();
            AddDefaultValues();
            AddForeignKeys();
            void AlterNotnullColumns()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    foreach (var column in table.Columns)
                    {
                        if (!column.IsNullable)
                        {
                            var columnName = PostgresUtils.IdentityName(column.Name);
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {columnName} SET NOT NULL;");
                        }
                    }
                }
            }
            void AddDefaultValues()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    foreach (var column in table.Columns)
                    {
                        if (!string.IsNullOrEmpty(column.DefaultValueSql))
                        {
                            var columnName = PostgresUtils.IdentityName(column.Name);
                            dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {columnName} SET DEFAULT {column.DefaultValueSql};");
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
                        var foreignKeyName = PostgresUtils.IdentityName(foreignKey.Name);
                        var foreignColumns = string.Join(", ", foreignKey.ColumnNames.Select(PostgresUtils.IdentityName));
                        var principalColumns = string.Join(", ", foreignKey.PrincipalNames.Select(PostgresUtils.IdentityName));
                        var principalTable = PostgresUtils.IdentityName(foreignKey.PrincipalSchema, foreignKey.PrincipalTable);
                        var sql =
                            $"ALTER TABLE {tableName} ADD CONSTRAINT {foreignKeyName} FOREIGN KEY ({foreignColumns}) REFERENCES {principalTable}({principalColumns})";
                        migrationContext.CreateTargetObject(sql, ObjectType.ForeignKey, foreignKeyName, tableName);
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
