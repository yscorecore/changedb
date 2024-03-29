﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ChangeDB.Import;
using ChangeDB.Import.ContentReaders;
using ChangeDB.Import.LineHanders;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerSqlScriptExecutor : BaseSqlScriptExecutor
    {
        //protected override IDictionary<char, char> ContentWrapChars()
        //{
        //    return new Dictionary<char, char>
        //    {
        //        ['\''] = '\'',
        //        ['"'] = '"',
        //        ['['] = ']'
        //    };
        //}
        //protected override (bool IsEnd, string sqlSegment) IsEndOfSqlSentence(string line)
        //{
        //    var isEnd = string.Equals(line.Trim(), "go", StringComparison.InvariantCultureIgnoreCase);
        //    return (isEnd, null);
        //}
        protected override IDictionary<char, IContentReader> ContentReaders()
        {
            return new Dictionary<char, IContentReader>
            {
                ['-'] = new CommentContentReader(),
                ['\''] = new QuoteContentReader('\'', '\''),
                ['"'] = new QuoteContentReader('"', '"'),
                ['['] = new QuoteContentReader('[', ']')
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
