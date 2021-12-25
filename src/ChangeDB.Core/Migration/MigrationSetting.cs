using System;
using System.Collections.Generic;

namespace ChangeDB.Migration
{
    public class MigrationSetting
    {
        public int FetchDataMaxSize { get; set; } = 1024 * 10;
        public MigrationType MigrationType { get; set; } = MigrationType.All;
        public bool DropTargetDatabaseIfExists { get; set; } = true;
        public SourceFilter SourceFilter { get; set; } = new SourceFilter();
        public CustomSqlScripts PostScripts { get; set; } = new CustomSqlScripts();
        public TargetNameStyle TargetNameStyle { get; set; } = new TargetNameStyle();

        public bool FixObjectNameMaxLength { get; set; } = true;

        public bool IncludeMeta { get => MigrationType.HasFlag(MigrationType.MetaData); }
        public bool IncludeData { get => MigrationType.HasFlag(MigrationType.MetaData); }

        public int GrowthSpeed { get => 10; }

        public int MaxTaskCount { get; set; } = 5;
    }

    [Flags]
    public enum MigrationType
    {
        MetaData = 1,
        Data = 2,
        All = MetaData | Data,
    }


    public class CustomSqlScripts
    {
        public List<string> SqlFiles { get; set; }

        public string SqlSplit { get; set; } = ";;";
    }

    public class SourceFilter
    {
        public List<string> SourceTablesPattern { get; set; } = new List<string> { "*" };
        public List<string> SourceTableFilter { get; set; } = new List<string> { };
    }
    public class TargetNameStyle
    {
        static readonly Random Random = new();
        static Func<string, string> Lower = p => p?.ToLowerInvariant();
        static Func<string, string> Upper = p => p?.ToUpperInvariant();
        static Func<string, string> Origin = p => p;

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
}
