using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB
{
    public interface IDatabaseManager
    {
        Task CreateDatabase(string connectionString, MigrationSetting setting);

        Task DropTargetDatabaseIfExists(string connectionString, MigrationSetting setting);
    }
}
