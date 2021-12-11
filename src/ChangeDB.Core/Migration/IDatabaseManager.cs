using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDatabaseManager
    {
        Task DropTargetDatabaseIfExists(MigrationContext migrationContext);

        Task CreateTargetDatabase(MigrationContext migrationContext);

    }
}
