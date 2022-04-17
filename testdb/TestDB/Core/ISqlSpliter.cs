using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface ISqlSpliter
    {
        IEnumerable<string> SplitReader(TextReader reader);
    }

    public static class SqlSpliterExtensions
    {
        public static IEnumerable<string> SplitFile(this ISqlSpliter sqlSpliter, string file)
        {
            using var reader = new StreamReader(file);
            foreach (var sql in sqlSpliter.SplitReader(reader))
            {
                yield return sql;
            }
        }
    }
}
