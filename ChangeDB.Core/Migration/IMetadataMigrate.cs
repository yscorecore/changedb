using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IMetadataMigrate
    {
        Task PreTransfer(MigrationContext context);

        Task PostTransfer(MigrationContext context);
    }
}
