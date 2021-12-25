using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.SqlServer.SqlServerUtils;
namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new SqlServerDatabaseManager();

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
