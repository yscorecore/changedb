using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDataMigrator
    {
        Task BeforeWriteTableData(TableDescriptor tableDescriptor, DbConnection connection, MigrationContext migrationContext);

        Task AfterWriteTableData(TableDescriptor tableDescriptor, DbConnection connection, MigrationContext migrationContext);

        Task<DataTable> ReadTableData(TableDescriptor table, PageInfo pageInfo, DbConnection connection, MigrationContext migrationContext);

        Task<long> CountTable(TableDescriptor table, DbConnection connection, MigrationContext migrationContext);

        Task WriteTableData(DataTable data, TableDescriptor table, DbConnection connection, MigrationContext migrationContext);

    }
}
