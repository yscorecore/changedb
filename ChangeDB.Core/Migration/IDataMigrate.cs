using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDataMigrate
    {
        Task TransferData(MigrationContext context);

    }
}
