using System.IO;
using System.Text;

namespace ChangeDB.Import.ContentReaders
{
    public class QuoteContentReader : IContentReader
    {
        private readonly char startChar;

        private readonly char endChar;

        private readonly bool supportSlashChar = false;
        public QuoteContentReader(char startChar, char endChar, bool supportSlashChar = false)
        {
            this.startChar = startChar;
            this.endChar = endChar;
        }

        public string ReadContent(TextReader reader)
        {
            var ch = reader.Peek();
            if (ch != startChar)
            {
                return string.Empty;
            }

            return supportSlashChar ?
                ReadContentInternalWithSlash(reader) :
                ReadContentInternal(reader);

        }
        private string ReadContentInternalWithSlash(TextReader reader)
        {
            var sb = new StringBuilder();
            sb.Append((char)reader.Read());
            bool inSlash = false;
            while (true)
            {
                var ch = reader.Read();
                if (ch == -1)
                {
                    return sb.ToString();
                }
                else if (ch == '\\')
                {
                    inSlash = !inSlash;
                }
                else
                {
                    if (ch == endChar && inSlash == false)
                    {
                        sb.Append((char)ch);
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append((char)ch);
                    }
                    inSlash = false;
                }
            }
        }
        private string ReadContentInternal(TextReader reader)
        {
            var sb = new StringBuilder();
            sb.Append((char)reader.Read());
            while (true)
            {
                var ch = reader.Read();
                if (ch == -1)
                {
                    return sb.ToString();
                }
                else if (ch == endChar)
                {
                    sb.Append((char)ch);
                    return sb.ToString();
                }
                else
                {
                    sb.Append((char)ch);
                }
            }
        }
    }
}
