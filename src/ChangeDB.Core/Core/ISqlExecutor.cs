using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace ChangeDB
{
    public interface ISqlExecutor
    {
        Task ExecuteReader(TextReader textReader, AgentContext agentContext);

    }

    public static class SqlExecutorExtensions
    {
        public static Task ExecuteFile(this ISqlExecutor executor, string file, AgentContext agentContext)
        {
            using var sqlReader = new StreamReader(file);
            return executor.ExecuteReader(sqlReader, agentContext);
        }

        public static Task ExecuteSqls(this ISqlExecutor executor, string sql, AgentContext agentContext)
        {
            using var sqlReader = new StringReader(sql ?? string.Empty);
            return executor.ExecuteReader(sqlReader, agentContext);
        }
    }
}
