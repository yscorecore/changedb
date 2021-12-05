using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Default
{
    public class DefaultSqlFormatter : ISqlFormatter
    {
        public WordCase KeywordCase { get; set; } = WordCase.Upper;

        public Task<string> FormatSql(string sql)
        {
            var result = KeywordCase switch
            {
                WordCase.Lower => ScanSql(sql, p => p.ToLowerInvariant()),
                WordCase.Upper => ScanSql(sql, p => p.ToUpperInvariant()),
                WordCase.Title => ScanSql(sql, p => p[0..1].ToUpperInvariant() + p[1..].ToLowerInvariant()),
                _ => sql,
            };
            return Task.FromResult(result);
        }

        private static string ScanSql(string sql, Func<string, string> NameFunc)
        {
            var stringBuilder = new StringBuilder(sql.Length);
            var allChars = sql?.ToCharArray() ?? Array.Empty<char>();
            var sqlScan = new SqlScan(allChars);
            while (sqlScan.IsNotEnd())
            {
                var (startIndex, length, isKeyword) = sqlScan.Go();
                if (isKeyword && NameFunc != null)
                {
                    var text = NameFunc.Invoke(sql.Substring(startIndex, length));
                    stringBuilder.Append(text);
                }
                else
                {
                    stringBuilder.Append(allChars, startIndex, length);
                }
            }
            return stringBuilder.ToString();
        }

        private ref struct SqlScan
        {
            public SqlScan(char[] chars)
            {
                this.chars = new Span<char>(chars);
                index = 0;
            }
            private Span<char> chars;
            private int index;
            public bool IsNotEnd()
            {
                return index < chars.Length;
            }
            private bool IsLetter()
            {
                return IsNotEnd() && char.IsLetter(chars[index]);
            }
            private void SkipWord()
            {
                while (IsLetter())
                {
                    index++;
                }
            }
            private bool Is(char ch) => IsNotEnd() && chars[index] == ch;

            private bool IsStringWrapChar() => Is('\'');

            private void SkipStringValue()
            {
                index++;
                while (IsNotEnd())
                {

                    if (IsStringWrapChar())
                    {
                        index++;
                        if (IsStringWrapChar())
                        {
                            index++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            private bool IsDoubleQuotation() => Is('\"');
            private bool IsBackQuote() => Is('`');
            private bool IsLeftBracket() => Is('[');

            private void SkipAfter(char ch)
            {
                while (IsNotEnd())
                {

                    if (Is(ch))
                    {
                        index++;
                        break;
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            private void SkipDoubleQuotation()
            {
                index++;
                SkipAfter('\"');
            }
            private void SkipRightBracket()
            {
                index++;
                SkipAfter(']');
            }
            private void SkipBackQuote()
            {
                index++;
                SkipAfter('`');
            }


            public (int startIndex, int length, bool isKeyword) Go()
            {
                var starIndex = index;

                if (IsLetter())
                {
                    SkipWord();
                    return (starIndex, index - starIndex, true);
                }
                else if (IsStringWrapChar())
                {
                    SkipStringValue();
                    return (starIndex, index - starIndex, false);
                }
                else if (IsDoubleQuotation())
                {
                    SkipDoubleQuotation();
                    return (starIndex, index - starIndex, false);
                }
                else if (IsLeftBracket())
                {
                    SkipRightBracket();
                    return (starIndex, index - starIndex, false);
                }
                else if (IsBackQuote())
                {
                    SkipBackQuote();
                    return (starIndex, index - starIndex, false);
                }
                else
                {
                    index++;
                    return (starIndex, index - starIndex, false);
                }
            }
        }
    }
    public enum WordCase
    {
        Lower,
        Upper,
        Title
    }
}
