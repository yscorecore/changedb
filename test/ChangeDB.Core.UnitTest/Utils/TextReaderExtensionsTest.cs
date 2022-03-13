using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Core.Utils
{
    public class TextReaderExtensionsTest
    {
        [Theory]
        [InlineData("", new string[0])]
        [InlineData("\r", new[] { "" })]
        [InlineData("\n", new[] { "" })]
        [InlineData("\r\n", new[] { "" })]
        [InlineData("\r\r", new[] { "", "" })]
        [InlineData("\n\n", new[] { "", "" })]
        [InlineData("abc", new[] { "abc" })]
        [InlineData("abc\n", new[] { "abc" })]
        [InlineData("abc\r", new[] { "abc" })]
        [InlineData("abc\r\n", new[] { "abc" })]
        [InlineData("abc\n\n", new[] { "abc", "" })]
        [InlineData("abc\n\r", new[] { "abc", "" })]
        [InlineData("abc\r\r", new[] { "abc", "" })]
        [InlineData("\nabc", new[] { "", "abc" })]
        [InlineData("\rabc", new[] { "", "abc" })]
        [InlineData("\r\nabc", new[] { "", "abc" })]
        [InlineData("\r\rabc", new[] { "", "", "abc" })]
        [InlineData("\n\nabc", new[] { "", "", "abc" })]
        [InlineData("\n\rabc", new[] { "", "", "abc" })]
        [InlineData("\n\rabc\nbcd\n\ncde\n\n", new[] { "", "", "abc", "bcd", "", "cde", "" })]
        [InlineData("\nabc'aa\n\nbb'\ncc", new[] { "", "abc'aa", "", "bb'", "cc" })]
        public void ShouldReadLine(string input, string[] expectedLines)
        {
            using var reader = new StringReader(input);
            reader.ReadAllLinesWithContent(null).Should().BeEquivalentTo(expectedLines);
        }
        [Theory]
        [InlineData("", new string[0])]
        [InlineData("\r", new[] { "" })]
        [InlineData("\n", new[] { "" })]
        [InlineData("\r\n", new[] { "" })]
        [InlineData("\r\r", new[] { "", "" })]
        [InlineData("\n\n", new[] { "", "" })]
        [InlineData("abc", new[] { "abc" })]
        [InlineData("abc\n", new[] { "abc" })]
        [InlineData("abc\r", new[] { "abc" })]
        [InlineData("abc\r\n", new[] { "abc" })]
        [InlineData("abc\n\n", new[] { "abc", "" })]
        [InlineData("abc\n\r", new[] { "abc", "" })]
        [InlineData("abc\r\r", new[] { "abc", "" })]
        [InlineData("\nabc", new[] { "", "abc" })]
        [InlineData("\rabc", new[] { "", "abc" })]
        [InlineData("\r\nabc", new[] { "", "abc" })]
        [InlineData("\r\rabc", new[] { "", "", "abc" })]
        [InlineData("\n\nabc", new[] { "", "", "abc" })]
        [InlineData("\n\rabc", new[] { "", "", "abc" })]
        [InlineData("\n\rabc\nbcd\n\ncde\n\n", new[] { "", "", "abc", "bcd", "", "cde", "" })]
        [InlineData("\nabc'aa\n\nbb'\ncc", new[] { "", "abc'aa\n\nbb'", "cc" })]
        [InlineData("\nabc'aa\n''\nbb'\n'cc", new[] { "", "abc'aa\n''\nbb'", "'cc" })]
        [InlineData("\n''''''''\n", new[] { "", "''''''''" })]
        [InlineData("\n'''''''\n'", new[] { "", "'''''''\n'" })]
        [InlineData("\n'''''''\n'\n", new[] { "", "'''''''\n'" })]
        [InlineData("\n'''''''\n'aa", new[] { "", "'''''''\n'aa" })]
        [InlineData("'", new[] { "'" })]
        [InlineData("'\n\n'\r", new[] { "'\n\n'" })]
        public void ShouldReadLineWithSingleQuoteContent(string input, string[] expectedLines)
        {
            using var reader = new StringReader(input);
            var readers = new Dictionary<char, IContentReader>
            {
                ['\''] = new TestSingleQuoteContentReader()
            };
            reader.ReadAllLinesWithContent(readers).Should().BeEquivalentTo(expectedLines);
        }

        class TestSingleQuoteContentReader : IContentReader
        {
            public string ReadContent(TextReader reader)
            {
                if (reader.Peek() == '\'')
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
                        sb.Append((char)ch);
                        if (ch == '\'')
                        {
                            return sb.ToString();
                        }
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
