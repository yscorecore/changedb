using System;
using ChangeDB.Migration;

namespace ChangeDB.Import
{
    public class ImportContext
    {
        public DatabaseInfo TargetDatabase { get; init; }
        public CustomSqlScript SqlScripts { get; set; }
        public bool ReCreateTargetDatabase { get; set; } = false;

        public event EventHandler<ObjectInfo> ObjectCreated;

        public event EventHandler<SqlSegmentInfo> SqlExecuted;

        public void ReportSqlExecuted(int startLine, int lineCount, string sql, int result)
        {
            SqlExecuted?.Invoke(this, new SqlSegmentInfo
            {
                LineCount = lineCount,
                Sql = sql,
                Result = result,
                StartLine = startLine
            });
        }

        public void ReportObjectCreated(ObjectInfo objectInfo)
        {
            ObjectCreated?.Invoke(this, objectInfo);
        }
    }

}
