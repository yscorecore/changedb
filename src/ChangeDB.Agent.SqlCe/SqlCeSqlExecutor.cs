﻿using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ChangeDB.Import;
using ChangeDB.Import.ContentReaders;
using ChangeDB.Import.LineHanders;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeSqlExecutor : BaseSqlExecutor
    {
        protected override IDictionary<char, IContentReader> ContentReaders()
        {
            return new Dictionary<char, IContentReader>
            {
                ['-'] = new CommentContentReader(),
                ['\''] = new QuoteContentReader('\'', '\''),
                ['"'] = new QuoteContentReader('"', '"'),
            };
        }

        protected override ISqlLineHandler[] SqlScriptHandlers()
        {
            return new ISqlLineHandler[]
            {
                NopLineHandler.EmptyLine,

                NopLineHandler.CommentLine,

                new NopLineHandler(@"^\s*go\s*$",RegexOptions.IgnoreCase),

                new CommandLineHandler(@"^\s*(go)?\s*$",RegexOptions.IgnoreCase)

            };
        }
    }
}