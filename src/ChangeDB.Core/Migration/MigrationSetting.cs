using System;
using System.Data;
using System.Data.Common;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Migration
{
    public record MigrationSetting
    {
        public DatabaseInfo SourceDatabase { get; init; }
        public DatabaseInfo TargetDatabase { get; init; }

        public int FetchDataMaxSize { get; set; } = 1024 * 10;
        public MigrationScope MigrationScope { get; set; } = MigrationScope.All;
        public bool DropTargetDatabaseIfExists { get; set; } = true;
        public CustomSqlScript PostScript { get; set; } = new CustomSqlScript();
        public CustomSqlScript PreScript { get; set; } = new CustomSqlScript();
        public TargetNameStyle TargetNameStyle { get; set; } = new TargetNameStyle();


        public bool IncludeMeta { get => MigrationScope.HasFlag(MigrationScope.MetaData); }
        public bool IncludeData { get => MigrationScope.HasFlag(MigrationScope.Data); }

        public int GrowthSpeed { get; set; } = 10;

        public int MaxTaskCount { get; set; } = 8;

        public string TargetDefaultSchema { get; set; } = string.Empty;


        public bool OptimizeInsertion { get; set; } = true;

    }


 public enum InsertionKind
    {
        Default,
        SingleRow,
        BatchRow,
        BlockCopy
    }


    [Flags]
    public enum MigrationScope
    {
        MetaData = 1,
        Data = 2,
        All = MetaData | Data,
    }


    public class CustomSqlScript
    {
        public string SqlFile { get; set; }

        public string SqlSplit { get; set; } = ";;";
    }

    public class TargetNameStyle
    {
        static readonly Random Random = new();
        static readonly Func<string, string> Lower = p => p?.ToLowerInvariant();
        static readonly Func<string, string> Upper = p => p?.ToUpperInvariant();
        static readonly Func<string, string> Origin = p => p;

        public NameStyle NameStyle { get; set; }
        public NameStyle? SchemaNameStyle { get; set; }
        public NameStyle? TableNameStyle { get; set; }
        public NameStyle? ColumnNameStyle { get; set; }
        public NameStyle? IndexNameStyle { get; set; }
        public NameStyle? ForeignKeyNameStyle { get; set; }
        public NameStyle? UniqueNameStyle { get; set; }
        public NameStyle? SequenceNameStyle { get; set; }
        public NameStyle? PrimaryKeyNameStyle { get; set; }

        public bool KeepOriginalConstraintName { get; set; }

        public Func<string, string> SchemaNameFunc { get => NameStyleToFunc(SchemaNameStyle ?? NameStyle); }
        public Func<string, string> TableNameFunc { get => NameStyleToFunc(TableNameStyle ?? NameStyle); }
        public Func<string, string> ColumnNameFunc { get => NameStyleToFunc(ColumnNameStyle ?? NameStyle); }
        public Func<string, string> SequenceNameFunc { get => NameStyleToFunc(SequenceNameStyle ?? NameStyle); }

        public Func<string, string> IndexNameFunc { get => NameStyleToFunc(IndexNameStyle ?? NameStyle); }
        public Func<string, string> ForeignKeyNameFunc { get => NameStyleToFunc(ForeignKeyNameStyle ?? NameStyle); }
        public Func<string, string> UniqueNameFunc { get => NameStyleToFunc(UniqueNameStyle ?? NameStyle); }
        public Func<string, string> PrimaryKeyNameFunc { get => NameStyleToFunc(PrimaryKeyNameStyle ?? NameStyle); }

        private static Func<string, string> NameStyleToFunc(NameStyle nameStyle)
        {
            return nameStyle switch
            {
                NameStyle.Lower => Lower,
                NameStyle.Upper => Upper,
                _ => Origin
            };
        }

    }

    public class ObjectInfo : IEventInfo
    {
        public ObjectType ObjectType { get; set; }

        public string FullName { get; set; }

        public string OwnerName { get; set; }

    }



    public enum ObjectType
    {
        Database,
        Schema,
        Table,
        Index,
        ForeignKey,
        PrimaryKey,
        Unique,
        UniqueIndex

    }

    public class TableDataInfo : IEventInfo
    {
        public long TotalCount { get; set; }
        public long MigratedCount { get; set; }
        public string Table { get; set; }

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

    public record StageInfo : IEventInfo
    {
        public StageKind Stage { get; set; }

    }
}
