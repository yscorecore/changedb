using System;
using System.Data.Common;

namespace ChangeDB.Migration
{
    public record MigrationContext : System.IDisposable
    {
        public DatabaseInfo SourceDatabase { get; init; }
        public DatabaseInfo TargetDatabase { get; init; }
        public MigrationSetting Setting { get; init; } = new MigrationSetting();
        public EventReporter EventReporter { get; set; } = new EventReporter();
        public AgentRunTimeInfo Source { get; set; }
        public AgentRunTimeInfo Target { get; set; }

        public DbConnection TargetConnection { get; set; }
        public DbConnection SourceConnection { get; set; }

        public void Dispose()
        {
            TargetConnection?.Dispose();
            SourceConnection?.Dispose();
        }

        public MigrationContext Fork()
        {
            return this with
            {
                TargetConnection = Target?.Agent?.CreateConnection(TargetDatabase.ConnectionString),
                SourceConnection = Source?.Agent?.CreateConnection(SourceDatabase.ConnectionString),
            };
        }
    }



    public record EventReporter
    {
        public event EventHandler<ObjectInfo> ObjectCreated;

        public event EventHandler<TableDataInfo> TableDataMigrated;

        public event EventHandler<StageKind> StageChanged;

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

        public void RaiseTableDataMigrated(TableDescriptor table, long totalCount, long migratedCount, bool completed) =>
            RaiseTableDataMigrated(new TableDataInfo
            {
                Table = table,
                TotalCount = totalCount,
                MigratedCount = migratedCount,
                Completed = completed
            });


        public void RaiseStageChanged(StageKind stageKind) => this.StageChanged?.Invoke(this, stageKind);
    }

    public static class MigrationContextExtensions
    {
        public static void CreateTargetObject(this MigrationContext context, string sql, ObjectType objectType, string fullName, string ownerName = null)
        {
            context.TargetConnection.ExecuteNonQuery(sql);
            context.EventReporter.RaiseObjectCreated(new ObjectInfo { ObjectType = objectType, FullName = fullName, OwnerName = ownerName });

        }

        public static void RaiseObjectCreated(this MigrationContext context, ObjectType objectType, string objectName, string ownerName = null)
        {
            context.EventReporter.RaiseObjectCreated(objectType, objectName, ownerName);
        }
        public static void RaiseTableDataMigrated(this MigrationContext context, TableDataInfo tableDataInfo)
        {
            context.EventReporter.RaiseTableDataMigrated(tableDataInfo);
        }

        public static void RaiseTableDataMigrated(this MigrationContext context, TableDescriptor table, long totalCount,
            long migratedCount, bool completed) =>
            context.EventReporter.RaiseTableDataMigrated(table, totalCount, migratedCount, completed);


        public static void RaiseStageChanged(this MigrationContext context, StageKind stageKind) =>
            context.EventReporter.RaiseStageChanged(stageKind);


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
        public long TotalCount { get; set; }
        public long MigratedCount { get; set; }
        public TableDescriptor Table { get; set; }

        public bool Completed { get; set; }

    }

    public enum StageKind
    {
        StartingPreMeta,
        FinishedPreMeta,
        StartingTableData,
        FinishedTableData,
        StartingPostMeta,
        FinishedPostMeta
    }
}
