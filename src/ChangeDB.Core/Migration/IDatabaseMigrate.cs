using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDatabaseMigrate
    {
        Task MigrateDatabase(MigrationSetting setting, IEventReporter eventReporter);
    }
}
