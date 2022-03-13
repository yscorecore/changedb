using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace ChangeDB.Import
{
    public class SqlScriptReader
    {
        private readonly TextReader textReader;
        private string currentLine;
        private int currentLineIndex;
        private bool hasCached = false;

        public IDictionary<char, IContentReader> ContentReaders { get; }

        public SqlScriptReader(TextReader textReader, IDictionary<char, IContentReader> contentReaders)
        {
            this.textReader = textReader;
            ContentReaders = contentReaders;
        }


        public (int LineNo, string LineContent) PeekLineWithContent()
        {
            lock (this)
            {
                if (!hasCached)
                {
                    // TODO fix line no
                    currentLine = textReader.ReadLineWithContent(this.ContentReaders);
                    currentLineIndex++;
                    hasCached = true;
                }
                return (currentLineIndex, currentLine);
            }

        }
        public (int LineNo, string LineContent) ReadLineWithContent(bool includeBreakLineChars = false)
        {
            lock (this)
            {
                if (hasCached)
                {
                    hasCached = false;
                    return (currentLineIndex, currentLine);
                }
                var nextLine = textReader.ReadLineWithContent(this.ContentReaders);
                if (nextLine == null)
                {
                    return (currentLineIndex, null);
                }
                else
                {
                    return (currentLineIndex++, nextLine);

                }
            }
        }
        public (int? StartLine, int? LineCount, string Content) ReadUntilSplitLine(Func<string, bool> lineSpliter)
        {
            List<(int, string)> lines = new List<(int, string)>();
            while (true)
            {
                var line = this.PeekLineWithContent();
                if (line.LineContent == null)
                {
                    return BuildResult();
                }
                if (lineSpliter(line.LineContent))
                {
                    this.ReadLineWithContent();
                    return BuildResult();
                }
                lines.Add(this.ReadLineWithContent(true));
            }

            (int?, int?, string) BuildResult()
            {
                if (lines.Count == 0)
                {
                    return (null, null, null);
                }
                else
                {
                    return (lines.First().Item1, lines.Count, string.Join("", lines.Select(p => p.Item2)));
                }
            }
        }
        public (int? StartLine, int? LineCount, string Content) ReadUntilTail(string tail, IDictionary<char, object> contentParsers, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(tail)) throw new ArgumentNullException(nameof(tail));
            List<(int, string)> lines = new List<(int, string)>();
            object contentParser = null;
            while (true)
            {


                if (contentParser != null)
                {

                }
                else
                {

                    var line = this.PeekLineWithContent();
                    if (line.LineContent == null)
                    {
                        return BuildResult();
                    }

                    foreach (var kv in contentParsers)
                    {

                    }
                    var index = line.LineContent.IndexOf(tail, stringComparison);
                    if (index >= 0)
                    {
                        lines.Add((line.LineNo, line.LineContent.Substring(0, index)));
                        return BuildResult();
                    }
                    lines.Add(this.ReadLineWithContent(true));
                }



            }

            (int?, int?, string) BuildResult()
            {
                if (lines.Count == 0)
                {
                    return (null, null, null);
                }
                else
                {
                    return (lines.First().Item1, lines.Count, string.Join("", lines.Select(p => p.Item2)));
                }
            }
        }



        //public IEnumerator<char> GetEnumerator()
        //{
        //    lock (this)
        //    {
        //        if (this.hasCached)
        //        {
        //            foreach (var ch in this.currentLine ?? string.Empty)
        //            {
        //                yield return ch;
        //            }
        //            this.hasCached = false;
        //        }

        //        while (true)
        //        {
        //            int ch = textReader.Read();
        //            if (ch == -1)
        //            {
        //                break;
        //            }
        //            else
        //            {
        //                yield return (char)ch;
        //            }
        //        }
        //    }
        //}

        //IEnumerator System.Collections.IEnumerable.GetEnumerator()
        //{
        //    return this.GetEnumerator();
        //}
    }
}
