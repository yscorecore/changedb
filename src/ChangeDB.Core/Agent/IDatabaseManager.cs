using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB
{
    public interface IDatabaseManager
    {
        Task CreateDatabase(string connectionString, MigrationSetting migrationSetting);

        Task DropTargetDatabaseIfExists(string connectionString, MigrationSetting migrationSetting);
    }
}
