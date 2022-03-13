using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace ChangeDB.Import
{
    public abstract class BaseSqlScriptExecutor : ISqlScriptExecutor
    {
        protected abstract IDictionary<char, IContentReader> ContentReaders();

        protected abstract ISqlLineHandler[] SqlScriptHandlers();
        public Task ExecuteReader(TextReader textReader, IDbConnection connection)
        {
            var scriptReader = new SqlScriptReader(textReader, ContentReaders());
            var context = new SqlScriptContext
            {
                Reader = scriptReader,
                Connection = connection
            };
            var handlers = SqlScriptHandlers() ?? Array.Empty<ISqlLineHandler>();
            while (true)
            {
                var (lineNo, line) = scriptReader.PeekLineWithContent();
                if (line == null)
                {
                    return Task.CompletedTask;
                }
                var handled = false;
                foreach (var handler in handlers)
                {
                    handler.Handle(context);
                    var (lineNo2, line2) = scriptReader.PeekLineWithContent();
                    if (line2 == null)
                    {
                        return Task.CompletedTask;
                    }
                    if (lineNo2 != lineNo)
                    {
                        handled = true;
                    }

                }
                if (handled == false)
                {
                    // should not go here
                    throw new NotSupportedException($"can not handle line {lineNo}, content :{line}");
                }
            }

        }


    }
}
