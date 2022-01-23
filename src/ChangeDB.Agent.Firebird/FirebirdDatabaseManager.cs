using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Firebird
{
    public class FirebirdDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new FirebirdDatabaseManager();
        public Task DropTargetDatabaseIfExists(MigrationContext migrationContext)
        {
            migrationContext.TargetConnection.DropDatabaseIfExists();
            return Task.CompletedTask;
        }

        public Task CreateTargetDatabase(MigrationContext migrationContext)
        {
            migrationContext.TargetConnection.CreateDatabase();
            migrationContext.RaiseObjectCreated(ObjectType.Database, migrationContext.TargetConnection.Database);
            return Task.CompletedTask;
        }
    }
}
