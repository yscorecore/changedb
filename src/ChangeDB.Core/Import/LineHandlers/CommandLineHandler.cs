using System.Text;
using System.Text.RegularExpressions;
using ChangeDB.Migration;

namespace ChangeDB.Import.LineHanders
{
    public class CommandLineHandler : ISqlLineHandler
    {
        private readonly Regex regex;
        public CommandLineHandler(string regex, RegexOptions regexOptions = default)
        {
            this.regex = new Regex(regex, regexOptions);
        }
        public void Handle(SqlScriptContext context)
        {
            var (lineNo, lineContent) = context.Reader.PeekLineWithContent();
            // should start with a whole word
            if (lineContent == null)
            {
                return;
            }
            var sb = new StringBuilder();
            while (true)
            {
                var (lineNo2, lineContent2) = context.Reader.PeekLineWithContent();
                if (lineContent2 == null || regex.IsMatch(lineContent2))
                {
                    var command = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(command))
                    {
                        context.Connection.ExecuteNonQuery(command);
                    }
                    return;
                }
                else
                {
                    sb.Append(lineContent2 + "\n");
                    context.Reader.ReadLineWithContent();
                }
            }

        }

    }
}
