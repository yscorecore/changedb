using System;

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
        public void RaiseTableDataMigrated(TableDataInfo tableDataInfo)
        {
            TableDataMigrated?.Invoke(this, tableDataInfo);
        }

        public AgentRunTimeInfo Source { get; internal set; }
        public AgentRunTimeInfo Target { get; internal set; }
    }
    public class ObjectInfo
    {
        public ObjectType Type { get; set; }

        public string FullName { get; set; }

    }

    public class ObjectMappingInfo
    {
        public ObjectInfo SourceObject { get; set; }
        public ObjectInfo TargetTarget { get; set; }

    }

    public enum ObjectType
    {
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
