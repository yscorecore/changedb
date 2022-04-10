using System.Data;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDatabaseManager
    {
        Task CleanDatabase(IDbConnection connection, MigrationSetting migrationSetting);

        Task CreateDatabase(string connectionString, MigrationSetting migrationSetting);

        Task DropTargetDatabaseIfExists(string connectionString, MigrationSetting migrationSetting);
    }
}
