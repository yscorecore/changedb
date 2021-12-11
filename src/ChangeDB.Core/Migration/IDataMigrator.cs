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

    }
}
