using System.IO;
using System.Text;

namespace ChangeDB.Import.ContentReaders
{
    public class CommentContentReader : IContentReader
    {
        public string ReadContent(TextReader reader)
        {
            if (reader.Peek() == '-')
            {
                var sb = new StringBuilder();
                sb.Append((char)reader.Read());

                var nextChar = reader.Peek();
                if (nextChar == '-')
                {
                    // read until line break
                    sb.Append(reader.ReadLine());
                    return sb.ToString();
                }
                else
                {
                    return sb.ToString();
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
