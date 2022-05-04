using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ChangeDB.Import;
using ChangeDB.Import.ContentReaders;
using ChangeDB.Import.LineHanders;

namespace ChangeDB.Agent.MySql
{
    public class MySqlSqlExecutor : BaseSqlExecutor
    {
        protected override IDictionary<char, IContentReader> ContentReaders()
        {
            return new Dictionary<char, IContentReader>
            {
                ['-'] = new CommentContentReader(),
                ['\''] = new QuoteContentReader('\'', '\''),
                ['"'] = new QuoteContentReader('"', '"'),
                ['`'] = new QuoteContentReader('`', '`')
            };
        }

        protected override ISqlLineHandler[] SqlScriptHandlers()
        {
            return new ISqlLineHandler[]
            {
                NopLineHandler.EmptyLine,

                NopLineHandler.CommentLine,

                new CommandLineHandler(@"^\s*\s*$",RegexOptions.IgnoreCase)

            };
        }
    }
}
