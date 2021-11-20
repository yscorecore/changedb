﻿using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDataMigrator
    {
        Task BeforeWriteTableData(TableDescriptor tableDescriptor, DbConnection connection, MigrationSetting migrationSetting);

        Task AfterWriteTableData(TableDescriptor tableDescriptor, DbConnection connection, MigrationSetting migrationSetting);

        Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, DbConnection connection, MigrationSetting migrationSetting);

        Task<long> CountTable(TableDescriptor table, DbConnection connection, MigrationSetting migrationSetting);

        Task WriteTableData(DataTable data, TableDescriptor table, DbConnection connection, MigrationSetting migrationSetting);

    }
}
