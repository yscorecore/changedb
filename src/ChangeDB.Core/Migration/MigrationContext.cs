using System;
using System.Data.Common;

namespace ChangeDB.Migration
{
    public record MigrationContext
    {
        public DatabaseInfo SourceDatabase { get; init; }
        public DatabaseInfo TargetDatabase { get; init; }
        public MigrationSetting Setting { get; init; } = new MigrationSetting();

        public event EventHandler<ObjectInfo> ObjectCreated;

        public event EventHandler<TableDataInfo> TableDataMigrated;

        public void RaiseObjectCreated(ObjectInfo objectInfo)
        {
            ObjectCreated?.Invoke(this, objectInfo);
        }
        public void RaiseObjectCreated(ObjectType objectType, string objectName, string ownerName = null)
        {
            this.RaiseObjectCreated(new ObjectInfo { ObjectType = objectType, FullName = objectName, OwnerName = ownerName });
        }
        public void RaiseTableDataMigrated(TableDataInfo tableDataInfo)
        {
            TableDataMigrated?.Invoke(this, tableDataInfo);
        }

        public AgentRunTimeInfo Source { get; set; }
        public AgentRunTimeInfo Target { get; set; }

        public DbConnection TargetConnection { get => Target.Connection; }
        public DbConnection SourceConnection { get => Source.Connection; }
    }
    public static class MigrationContextExtensions
    {
        public static void CreateTargetObject(this MigrationContext context, string sql, ObjectType objectType, string fullName, string ownerName = null)
        {
            context.TargetConnection.ExecuteNonQuery(sql);
            context.RaiseObjectCreated(new ObjectInfo { ObjectType = objectType, FullName = fullName, OwnerName = ownerName });

        }

    }
    public class ObjectInfo
    {
        public ObjectType ObjectType { get; set; }

        public string FullName { get; set; }

        public string OwnerName { get; set; }

    }

    public class ObjectMappingInfo
    {
        public ObjectInfo SourceObject { get; set; }
        public ObjectInfo TargetTarget { get; set; }

    }

    public enum ObjectType
    {
        Database,
        Schema,
        Table,
        Index,
        ForeignKey,
        PrimaryKey,
        Unique

    }

    public class TableDataInfo
    {
        public int TotalCount { get; set; }
        public int MigratedCount { get; set; }
        public string TableFullName { get; set; }
    }
}
