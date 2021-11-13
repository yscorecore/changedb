using System.Collections.Generic;

namespace ChangeDB.Migration
{
    public class MigrationSetting
    {
        public bool IncludeMeta { get; set; } = true;
        public bool IncludeData { get; set; } = true;

        public int MaxPageSize { get; set; } = 10000;

        public bool DropTargetDatabaseIfExists { get; set; } = false;
        public SourceFilter SourceFilter { get; set; } = new SourceFilter();
        public CustomSqlScripts PostScripts { get; set; } = new CustomSqlScripts();
        public TargetNameStyle TargetNameStyle { get; set; } = new TargetNameStyle();
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
        public NameStyle NameStyle { get; set; }
        public NameStyle? SchemaNameStyle { get; set; }
        public NameStyle? TableNameStyle { get; set; }
        public NameStyle? ColumnNameStyle { get; set; }
        public NameStyle? IndexNameStyle { get; set; }
        public NameStyle? ForeignKeyNameStyle { get; set; }
        public NameStyle? UniqueNameStyle { get; set; }
    }
}
