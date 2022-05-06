using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB
{
    public interface IDatabaseManager
    {
        Task CreateDatabase(string connectionString);

        Task DropDatabaseIfExists(string connectionString);
    }
}
