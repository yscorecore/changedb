using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public interface IContentReader
    {
        string ReadContent(TextReader reader);
    }

    public static class TextReaderExtensions
    {
        public static IEnumerable<string> ReadAllLinesWithContent(this TextReader reader, IDictionary<char, IContentReader> contentReaders)
        {
            while (true)
            {
                var line = reader.ReadLineWithContent(contentReaders);
                if (line == null)
                {
                    break;
                }
                else
                {
                    yield return line;
                }
            }
        }

        public static string ReadLineWithContent(this TextReader reader, IDictionary<char, IContentReader> contentReaders)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));
            var sb = new StringBuilder();
            while (true)
            {
                int ch = reader.Peek();
                if (ch == -1) break;
                if (contentReaders != null && contentReaders.TryGetValue((char)ch, out var contentReader))
                {
                    sb.Append(contentReader.ReadContent(reader));
                }
                else
                {
                    reader.Read();
                    if (ch == '\n')
                    {
                        return sb.ToString();
                    }
                    else if (ch == '\r')
                    {
                        if (reader.Peek() == '\n')
                        {
                            reader.Read();
                        }
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append((char)ch);
                    }
                }
            }
            if (sb.Length > 0)
            {
                return sb.ToString();
            }

            return null;
        }

        public static void AppendReader(this TextWriter writer, TextReader reader)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            _ = reader ?? throw new ArgumentNullException(nameof(reader));
            while (true)
            {
                int ch = reader.Read();
                if (ch == -1) break;
                writer.Write((char)ch);
            }
        }

        public static IEnumerable<string> ReadAllLines(this TextReader reader)
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }
                else
                {
                    yield return line;
                }
            }
        }
    }
}
