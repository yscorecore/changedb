using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public abstract class BaseServiceProvider : IDatabaseServiceProvider
    {

        public abstract bool SupportFastClone { get; }
        public virtual long FastClonedMinimalSqlSize => 1024 * 64;

        public abstract string ChangeDatabase(string connectionString, string databaseName);

        public abstract void CleanDatabase(string connectionString);

        public abstract void CloneDatabase(string connectionString, string newDatabaseName);

        public abstract DbConnection CreateConnection(string connectionString);

        public abstract void CreateDatabase(string connectionString);

        public abstract void DropTargetDatabaseIfExists(string connectionString);

        public abstract string MakeReadOnly(string connectionString);

        protected abstract bool IsSplitLine(string line);

        public virtual IEnumerable<string> SplitReader(TextReader textReader)
        {
            var lines = new List<string>();
            while (true)
            {
                string line = textReader.ReadLine();
                if (line == null)
                {
                    var sql = ExecuteCurrentStringBuilder();

                    if (!string.IsNullOrEmpty(sql))
                    {
                        yield return sql;
                    }
                    break;
                }

                if (IsSplitLine(line))
                {
                    yield return ExecuteCurrentStringBuilder();
                }
                else
                {
                    lines.Add(line);
                }
            }


            string ExecuteCurrentStringBuilder()
            {
                var sql = string.Join('\n', lines);
                lines.Clear();
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    return sql;
                }
                return null;
            }


        }
    }
}
