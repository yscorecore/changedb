using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDataMigrator
    {
        Task BeforeWriteTargetTable(TableDescriptor tableDescriptor, MigrationContext migrationContext);

        Task AfterWriteTargetTable(TableDescriptor tableDescriptor, MigrationContext migrationContext);

        Task<DataTable> ReadSourceTable(TableDescriptor table, PageInfo pageInfo, MigrationContext migrationContext);

        Task<long> CountSourceTable(TableDescriptor table, MigrationContext migrationContext);

        Task WriteTargetTable(DataTable data, TableDescriptor table, MigrationContext migrationContext);

        [Obsolete]
        public async IAsyncEnumerable<DataTable> ReadSourceTable(TableDescriptor sourceTable, MigrationContext migrationContext)
        {
            var source = migrationContext.Source;
            var migrationSetting = migrationContext.Setting;

            var (loadedCount, maxRowSize, fetchCount) = (0, 1, 1);

            while (true)
            {
                var pageInfo = new PageInfo { Offset = loadedCount, Limit = Math.Max(1, fetchCount) };
                var dataTable = await source.Agent.DataMigrator.ReadSourceTable(sourceTable, pageInfo, migrationContext);

                yield return dataTable;

                loadedCount += dataTable.Rows.Count;
                maxRowSize = Math.Max(maxRowSize, dataTable.MaxRowSize());
                fetchCount = Math.Min(fetchCount * migrationSetting.GrowthSpeed, Math.Max(1, migrationSetting.FetchDataMaxSize / maxRowSize));

                if (dataTable.Rows.Count < pageInfo.Limit)
                {
                    break;
                }
            }
        }

        [Obsolete]
        public async IAsyncEnumerable<DataRow> ReadSourceRows(TableDescriptor sourceTable, MigrationContext migrationContext)
        {
            await foreach (DataTable table in ReadSourceTable(sourceTable, migrationContext))
            {
                foreach (DataRow row in table.Rows)
                {
                    yield return row;
                }
            }
        }
    }


}
