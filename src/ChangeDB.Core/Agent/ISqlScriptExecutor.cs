using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace ChangeDB
{
    public interface ISqlScriptExecutor
    {
        Task ExecuteReader(TextReader textReader, IDbConnection connection);

        public virtual Task ExecuteFile(string file, IDbConnection connection)
        {
            using var sqlReader = new StreamReader(file);
            return ExecuteReader(sqlReader, connection);
        }

        public virtual Task ExecuteSqls(string sql, IDbConnection connection)
        {
            using var sqlReader = new StringReader(sql ?? string.Empty);
            return ExecuteReader(sqlReader, connection);
        }

    }
}
