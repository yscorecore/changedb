using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDatabaseMigrate:IMetadataMigrate,IDataMigrate
    {
        Task MigrateDatabase(MigrationContext context);
    }
}
