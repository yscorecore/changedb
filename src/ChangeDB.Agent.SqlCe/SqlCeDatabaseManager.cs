﻿using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.SqlCe.SqlCeUtils;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new SqlCeDatabaseManager();

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
