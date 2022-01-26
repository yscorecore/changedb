using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.MySql.MySqlUtils;
namespace ChangeDB.Agent.MySql
{
    public class MySqlDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new MySqlDatabaseManager();
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
