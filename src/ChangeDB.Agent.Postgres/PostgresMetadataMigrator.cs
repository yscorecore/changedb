using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;


namespace ChangeDB.Agent.Postgres
{
    public class PostgresMetadataMigrator : IMetadataMigrator
    {
        public static readonly PostgresMetadataMigrator Default = new PostgresMetadataMigrator();

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
            AlterIdentityColumns();
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
                            dbConnection.ExecuteNonQuery($"alter table {tableFullName} alter column {columnName} set not null;");
                        }
                    }
                }
            }
            void AlterIdentityColumns()
            {
                foreach (var table in databaseDescriptor.Tables)
                {
                    var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                    foreach (var column in table.Columns)
                    {
                        if (column.IdentityInfo != null)
                        {
                            var columnName = PostgresUtils.IdentityName(column.Name);
                            if (column.IsIdentity)
                            {
                                AlterIdentityColumn(column.IdentityInfo, tableFullName, columnName);
                            }
                            else
                            {
                                var sequenceName = $"{table.Name}_{column.Name}_seq";
                                var sequenceFullName = PostgresUtils.IdentityName(table.Schema, sequenceName);
                                var columnFullName = PostgresUtils.IdentityName(table.Schema, table.Name, column.Name);
                                var nextValueExpression = $"nextval('{sequenceFullName}'::regclass)";
                                var sequenceDetails = BuildIdentitySequenceDetails(column.IdentityInfo, false, columnFullName);
                                dbConnection.ExecuteNonQuery($"CREATE SEQUENCE IF NOT EXISTS {sequenceFullName} {sequenceDetails};");
                                dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER COLUMN {columnName} SET DEFAULT {nextValueExpression};");
                            }
                        }
                    }
                }


            }
            void AlterIdentityColumn(IdentityDescriptor desc, string tableFullName, string columnName)
            {
                var identityType = "ALWAYS";
                if (desc != null && desc.Values != null && desc.Values.TryGetValue(PostgresUtils.IdentityType, out var type))
                {
                    identityType = Convert.ToString(type);
                }

                var identityDetails = string.Empty;
                if (desc != null)
                {
                    identityDetails = BuildIdentitySequenceDetails(desc);
                }

                var identityFullDesc = $"GENERATED {identityType} AS IDENTITY{identityDetails}";
                dbConnection.ExecuteNonQuery($"ALTER TABLE {tableFullName} ALTER {columnName} ADD {identityFullDesc};");
            }
            //https://www.postgresql.org/docs/current/sql-altersequence.html
            string BuildIdentitySequenceDetails(IdentityDescriptor desc, bool includeBrackets = true, string ownedBy = default)
            {
                var items = new List<string>();
                if (desc.IncrementBy != null)
                {
                    items.Add($"INCREMENT BY {desc.IncrementBy}");
                }
                if (desc.MinValue != null)
                {
                    items.Add($"MINVALUE {desc.MinValue}");
                }
                if (desc.MaxValue != null)
                {
                    items.Add($"MAXVALUE {desc.MaxValue}");
                }
                if (desc.StartValue != null)
                {
                    items.Add($"START WITH {desc.StartValue}");
                }
                if (desc.IsCyclic == true)
                {
                    items.Add("CYCLE");
                }
                if (desc.Values.TryGetValue(PostgresUtils.IdentityNumbersToCache, out var cache))
                {
                    items.Add($"CACHE {cache}");
                }
                if (!string.IsNullOrEmpty(ownedBy))
                {
                    items.Add($"OWNED BY {ownedBy}");
                }
                if (items.Count > 0)
                {
                    return includeBrackets ? $"({string.Join(" ", items)})" : string.Join(" ", items);
                }
                else
                {
                    return string.Empty;
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
