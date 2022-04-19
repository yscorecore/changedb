
using FluentAssertions;
using Xunit;

namespace ChangeDB.Core.Utils
{
    public class StringExtensionsTest
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("abc", "abc")]
        [InlineData(".", ".")]
        [InlineData(".abc", ".abc")]
        [InlineData("0", "0")]
        [InlineData("00", "00")]
        [InlineData("0.", "0")]
        [InlineData("00.", "00")]
        [InlineData("0.0", "0")]
        [InlineData(".0", ".0")]
        [InlineData(".000", ".0")]
        [InlineData("0.00", "0")]
        [InlineData("123", "123")]
        [InlineData("12300", "12300")]
        [InlineData("123.", "123")]
        [InlineData("123.0", "123")]
        [InlineData("123.10", "123.1")]
        [InlineData("123.01", "123.01")]
        [InlineData(".123", ".123")]
        [InlineData(".1230", ".123")]
        [InlineData("abc123.0", "abc123")]
        [InlineData("abc123.10", "abc123.1")]
        [InlineData("abc123.01", "abc123.01")]
        public void ShouldTrimZeroTail(string decimalText, string expected)
        {
            decimalText.TrimDecimalZeroTail().Should().Be(expected);
        }
    }
}
