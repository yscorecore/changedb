using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.Postgres.PostgresUtils;
namespace ChangeDB.Agent.Postgres
{
    public class PostgresDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new PostgresDatabaseManager();
        public Task DropTargetDatabaseIfExists(MigrationContext migrationContext)
        {
            migrationContext.TargetConnection.DropDatabaseIfExists();
            return Task.CompletedTask;
        }

        public Task CreateTargetDatabase(MigrationContext migrationContext)
        {
            migrationContext.TargetConnection.CreateDatabase();
            migrationContext.RaiseObjectCreated(ObjectType.Database, IdentityName(migrationContext.TargetConnection.Database));
            return Task.CompletedTask;
        }
    }
}
