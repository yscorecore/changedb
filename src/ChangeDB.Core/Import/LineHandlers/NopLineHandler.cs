using System.Text.RegularExpressions;

namespace ChangeDB.Import.LineHanders
{
    public class NopLineHandler : ISqlLineHandler
    {
        public static readonly NopLineHandler EmptyLine = new(@"^\s*$");
        public static readonly NopLineHandler CommentLine = new(@"^\s*--");
        public NopLineHandler(string regex, RegexOptions regexOptions = default)
        {
            this.regex = new Regex(regex, regexOptions);
        }
        private readonly Regex regex;

        public void Handle(SqlScriptContext context)
        {
            while (true)
            {
                var (_, content) = context.Reader.PeekLineWithContent();
                if (content == null)
                {
                    break;
                }
                if (regex.IsMatch(content))
                {
                    context.Reader.ReadLineWithContent();
                }
                else
                {
                    break;
                }
            }
        }
    }
}
