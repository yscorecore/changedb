using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresMetadataMigrator : IMetadataMigrator
    {
        public static readonly PostgresMetadataMigrator Default = new PostgresMetadataMigrator();

        //https://github.com/npgsql/npgsql/blob/5c5c31e4d9d35ce22f023b45a6bd4a4ba6668f33/src/Npgsql/NpgsqlSchema.cs#L231
        public Task<DatabaseDescriptor> GetDatabaseDescriptor(DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            var databaseName =dbConnection.ExtractDatabaseName();

            var reservedwords =dbConnection.GetSchema("RESERVEDWORDS");
            var tables =dbConnection.GetSchema("TABLES", new string[] { databaseName });
            var columns =dbConnection.GetSchema("COLUMNS", new string[] { databaseName });

            var names = Enumerable.Range(0, columns.Columns.Count).Select(p => columns.Columns[p].ColumnName).ToArray();


            var index =dbConnection.GetSchema("INDEXES", new string[] { databaseName });
            var indexColumns =dbConnection.GetSchema("INDEXCOLUMNS", new string[] { databaseName });
            var constraints =dbConnection.GetSchema("CONSTRAINTS", new string[] { databaseName });
            var prmarykeys = constraints.AsEnumerable().Where(p => p.Field<string>("CONSTRAINT_TYPE") == "PRIMARY KEY").ToArray();
            var forigenKeys = constraints.AsEnumerable().Where(p => p.Field<string>("CONSTRAINT_TYPE") == "FOREIGN KEY").ToArray();
            var uniqueKeys = constraints.AsEnumerable().Where(p => p.Field<string>("CONSTRAINT_TYPE") == "UNIQUE KEY").ToArray();
            var constraintColumns =dbConnection.GetSchema("CONSTRAINTCOLUMNS", new string[] { databaseName });
            var primaryKeyColumns = constraintColumns.AsEnumerable().Where(p => p.Field<string>("constraint_type") == "PRIMARY KEY").ToArray().AsEnumerable();
            var foreignKeyColumns = constraintColumns.AsEnumerable().Where(p => p.Field<string>("constraint_type") == "FOREIGN KEY").ToArray().AsEnumerable();
            var uniqueKeyColumns = constraintColumns.AsEnumerable().Where(p => p.Field<string>("constraint_type") == "UNIQUE KEY").ToArray().AsEnumerable();

            var databaseDesc = new DatabaseDescriptor
            {
                Tables = BuildTables(tables, columns, databaseName)
            };
            SetAllSchemas(databaseDesc);
            return Task.FromResult(databaseDesc);
            List<TableDescriptor> BuildTables(DataTable tableData, DataTable columnData, string dbname)
            {
                var tableList = tableData.AsEnumerable()
                     .Where(p => DBFilter(p, dbname))
                     .Select(p => new TableDescriptor
                     {
                         Name = Convert.ToString(p["table_name"]),
                         Schema = Convert.ToString(p["table_schema"]),
                     }).ToList();
                tableList.ForEach(t => { t.Columns = BuildColumns(columnData, dbname, t.Schema, t.Name); });
                return tableList;
            }
            List<ColumnDescriptor> BuildColumns(DataTable data, string dbname, string schemeName, string tableName)
            {
                var columnList = data.AsEnumerable()
                     .Where(p => TableFilter(p, dbname, schemeName, tableName))
                     .OrderBy(p => p.Field<int>("ordinal_position"))
                     .Select(p => new ColumnDescriptor
                     {
                         Name = p.Field<string>("column_name"),
                         DbType = CreateDbTypeDesc(p),
                         AllowNull = "YES".Equals(p.Field<string>("is_nullable"), StringComparison.InvariantCultureIgnoreCase),
                         DefaultValue = p.Field<string>("column_default")
                     }).ToList();
                columnList.ForEach(col => { col.IsPrimaryKey = primaryKeyColumns.Any(p => TableFilter(p, dbname, schemeName, tableName) && p.Field<string>("column_name") == col.Name); });

                return columnList;
            }
            bool DBFilter(DataRow p, string dbname)
            {
                return p.Field<string>("table_catalog") == dbname;
            }

            bool TableFilter(DataRow p, string dbname, string schemeName, string tableName)
            {
                return p.Field<string>("table_catalog") == dbname && p.Field<string>("table_schema") == schemeName && p.Field<string>("table_name") == tableName;
            }
            void SetAllSchemas(DatabaseDescriptor databaseDescriptor)
            {
                databaseDescriptor.Schemas = databaseDescriptor.Tables.Select(p => p.Schema).Distinct().ToList();
            }
        }


        public Task PreMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            CreateTargetDatabase(dbConnection, migrationSetting);
            CreateTargetSchemas(databaseDescriptor, dbConnection, migrationSetting);
            CreateTargetTablesWithoutConstraints(databaseDescriptor, dbConnection, migrationSetting);
            return Task.CompletedTask;
        }
        public Task PostMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            ApplyTargetTablesConstraints(databaseDescriptor, dbConnection, migrationSetting);
            return Task.CompletedTask;
        }

        private static void CreateTargetDatabase(DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            if (migrationSetting.DropTargetDatabaseIfExists)
            {
               dbConnection.ReCreateDatabase();
            }
            else
            {
               dbConnection.CreateDatabase();
            }
        }
        private static void CreateTargetSchemas(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            foreach (var schema in databaseDescriptor.Schemas)
            {
               dbConnection.ExecuteNonQuery($"CREATE SCHEMA IF NOT EXISTS {PostgresUtils.IdentityName(schema)};");
            }
        }
        private static void CreateTargetTablesWithoutConstraints(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            // create table
            // primary key
            // index
            foreach (var table in databaseDescriptor.Tables ?? Enumerable.Empty<TableDescriptor>())
            {
                var tableFullName = PostgresUtils.IdentityName(table.Schema, table.Name);
                var columns = string.Join(",", table.Columns.Select(p => $"{PostgresUtils.IdentityName(p.Name)} {TranformDataType(p.DbType)}"));
               dbConnection.ExecuteNonQuery(
                    $"CREATE TABLE {tableFullName} ({columns});");
            }

        }
        private static void ApplyTargetTablesConstraints(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            // not null
            // unique
            // forgin key
            // identity column
            // default value

        }
        private static DBTypeDescriptor CreateDbTypeDesc(DataRow row)
        {
            var dataType = row.Field<string>("data_type");
            var characterMaximumLength = row.Field<int?>("character_maximum_length");
            var characterOctetLength = row.Field<int?>("character_octet_length");
            var numericPrecision = row.Field<int?>("numeric_precision");
            var numericPrecisionRadix = row.Field<int?>("numeric_precision_radix");
            var numericScale = row.Field<int?>("numeric_scale");
            return dataType.ToUpperInvariant() switch
            {
                "CHARACTER VARYING" => new DBTypeDescriptor { DbType = DBType.Character__Varying, Length = characterMaximumLength },
                var x when x == "INTEGER" || x == "INT" => new DBTypeDescriptor { DbType = DBType.Int },
                _ => throw new NotSupportedException($"the data type '{dataType}' not supported.")
            };
        }

        private static string TranformDataType(DBTypeDescriptor dataType)
        {
            return dataType.ToString();
        }
    }
}
