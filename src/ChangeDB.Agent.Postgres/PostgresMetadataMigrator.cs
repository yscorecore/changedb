using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresMetadataMigrator : IMetadataMigrator
    {
        public static readonly PostgresMetadataMigrator Default = new PostgresMetadataMigrator();

        //https://github.com/npgsql/npgsql/blob/5c5c31e4d9d35ce22f023b45a6bd4a4ba6668f33/src/Npgsql/NpgsqlSchema.cs#L231
        public Task<DatabaseDescriptor> GetDatabaseDescriptor(DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            var databaseName = databaseInfo.Connection.ExtractDatabaseName();

            var reservedwords = databaseInfo.Connection.GetSchema("RESERVEDWORDS");
            var tables = databaseInfo.Connection.GetSchema("TABLES", new string[] { databaseName });
            var columns = databaseInfo.Connection.GetSchema("Columns", new string[] { databaseName });
            var index = databaseInfo.Connection.GetSchema("INDEXES", new string[] { databaseName });
            var indexColumns = databaseInfo.Connection.GetSchema("INDEXCOLUMNS", new string[] { databaseName });
            var constraints = databaseInfo.Connection.GetSchema("CONSTRAINTS", new string[] { databaseName });
            var prmarykeys = constraints.AsEnumerable().Where(p => p.Field<string>("CONSTRAINT_TYPE") == "PRIMARY KEY").ToArray();
            var forigenKeys = constraints.AsEnumerable().Where(p => p.Field<string>("CONSTRAINT_TYPE") == "FOREIGN KEY").ToArray();
            var uniqueKeys = constraints.AsEnumerable().Where(p => p.Field<string>("CONSTRAINT_TYPE") == "UNIQUE KEY").ToArray();
            var constraintColumns = databaseInfo.Connection.GetSchema("CONSTRAINTCOLUMNS", new string[] { databaseName });
            var primaryKeyColumns = constraintColumns.AsEnumerable().Where(p => p.Field<string>("constraint_type") == "PRIMARY KEY").ToArray().AsEnumerable();
            var foreignKeyColumns = constraintColumns.AsEnumerable().Where(p => p.Field<string>("constraint_type") == "FOREIGN KEY").ToArray().AsEnumerable();
            var uniqueKeyColumns = constraintColumns.AsEnumerable().Where(p => p.Field<string>("constraint_type") == "UNIQUE KEY").ToArray().AsEnumerable();

            var databaseDesc = new DatabaseDescriptor
            {
                Name = databaseName,
                Tables = BuildTables(tables, columns, databaseName)
            };
            SetAllSchemas(databaseDesc);
            return Task.FromResult(databaseDesc);
            List<TableDescriptor> BuildTables(DataTable tableData, DataTable columnData, string dbname)
            {
                var tableList = tableData.AsEnumerable()
                     .Where(p => DBFilter(p,dbname))
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
                     .Where(p => TableFilter(p,dbname,schemeName,tableName))
                     .OrderBy(p => p.Field<int>("ordinal_position"))
                     .Select(p => new ColumnDescriptor
                     {
                         Name = p.Field<string>("column_name"),
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



        public Task PreMigrate(DatabaseDescriptor databaseDescriptor, DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }

        public Task PostMigrate(DatabaseDescriptor databaseDescriptor, DatabaseInfo databaseInfo, MigrationSetting migrationSetting)
        {
            throw new System.NotImplementedException();
        }
    }
}
