using System.Collections.Generic;
using System.IO;

namespace ChangeDB.Migration
{
    public interface ISqlSplitter
    {
        IEnumerable<string> SplitSql(TextReader textReader);
    }
    public static class ISqlSplitterExtensions
    {
        public static IEnumerable<string> SplitSql(this ISqlSplitter sqlSplitter, string sql)
        {
            using var sqlReader = new StringReader(sql ?? string.Empty);
            return sqlSplitter.SplitSql(sqlReader);
        }
        public static IEnumerable<string> SplitSqlFile(this ISqlSplitter sqlSplitter, string sqlFileName)
        {
            using var sqlReader = new StreamReader(sqlFileName);
            return sqlSplitter.SplitSql(sqlReader);
        }
    }
}
