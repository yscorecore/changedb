using System.Threading.Tasks;
using ChangeDB.Default;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Core.Default
{
    public class DefaultSqlFormatterTest
    {
        [Theory]
        [InlineData("Insert Into [abc]([val]) Values(1)", "insert into [abc]([val]) values(1)")]
        [InlineData("Insert Into `abc`(`val`) Values(1)", "insert into `abc`(`val`) values(1)")]
        [InlineData("Insert Into \"abc\"(\"val\") Values(1)", "insert into \"abc\"(\"val\") values(1)")]
        [InlineData("Insert Into [dbo].[abc]([val]) Values(1)", "insert into [dbo].[abc]([val]) values(1)")]
        [InlineData("Insert Into `dbo`.`abc`(`val`) Values(1)", "insert into `dbo`.`abc`(`val`) values(1)")]
        [InlineData("Insert Into \"dbo\".\"abc\"(\"val\") Values(1)", "insert into \"dbo\".\"abc\"(\"val\") values(1)")]
        [InlineData("Insert Into [abc]([val]) Values(True)", "insert into [abc]([val]) values(true)")]
        [InlineData("Insert Into [abc]([val]) Values('ABC')", "insert into [abc]([val]) values('ABC')")]
        [InlineData("Insert Into [abc]([val]) Values('''ABCDE\"')", "insert into [abc]([val]) values('''ABCDE\"')")]

        public async Task ShouldFormattedSqlToLowerCase(string sql, string expectSql)
        {
            var formatter = new DefaultSqlFormatter() { KeywordCase = WordCase.Lower };

            var result = await formatter.FormatSql(sql);

            result.Should().Be(expectSql);
        }

        [Theory]
        [InlineData("Insert Into [abc]([val]) Values(1)", "INSERT INTO [abc]([val]) VALUES(1)")]
        [InlineData("Insert Into `abc`(`val`) Values(1)", "INSERT INTO `abc`(`val`) VALUES(1)")]
        [InlineData("Insert Into \"abc\"(\"val\") Values(1)", "INSERT INTO \"abc\"(\"val\") VALUES(1)")]
        [InlineData("Insert Into [dbo].[abc]([val]) Values(1)", "INSERT INTO [dbo].[abc]([val]) VALUES(1)")]
        [InlineData("Insert Into `dbo`.`abc`(`val`) Values(1)", "INSERT INTO `dbo`.`abc`(`val`) VALUES(1)")]
        [InlineData("Insert Into \"dbo\".\"abc\"(\"val\") Values(1)", "INSERT INTO \"dbo\".\"abc\"(\"val\") VALUES(1)")]
        [InlineData("Insert Into [abc]([val]) Values(True)", "INSERT INTO [abc]([val]) VALUES(TRUE)")]
        [InlineData("Insert Into [abc]([val]) Values('ABC')", "INSERT INTO [abc]([val]) VALUES('ABC')")]
        [InlineData("Insert Into [abc]([val]) Values('''ABCDE\"')", "INSERT INTO [abc]([val]) VALUES('''ABCDE\"')")]

        public async Task ShouldFormattedSqlToUpperCase(string sql, string expectSql)
        {
            var formatter = new DefaultSqlFormatter() { KeywordCase = WordCase.Upper };

            var result = await formatter.FormatSql(sql);

            result.Should().Be(expectSql);
        }

        [Theory]
        [InlineData("insert INTO [abc]([val]) Values(1)", "Insert Into [abc]([val]) Values(1)")]
        [InlineData("insert INTO `abc`(`val`) Values(1)", "Insert Into `abc`(`val`) Values(1)")]
        [InlineData("insert INTO \"abc\"(\"val\") Values(1)", "Insert Into \"abc\"(\"val\") Values(1)")]
        [InlineData("insert INTO [dbo].[abc]([val]) Values(1)", "Insert Into [dbo].[abc]([val]) Values(1)")]
        [InlineData("insert INTO `dbo`.`abc`(`val`) Values(1)", "Insert Into `dbo`.`abc`(`val`) Values(1)")]
        [InlineData("insert INTO \"dbo\".\"abc\"(\"val\") Values(1)", "Insert Into \"dbo\".\"abc\"(\"val\") Values(1)")]
        [InlineData("insert INTO [abc]([val]) Values(True)", "Insert Into [abc]([val]) Values(True)")]
        [InlineData("insert INTO [abc]([val]) Values('ABC')", "Insert Into [abc]([val]) Values('ABC')")]
        [InlineData("insert INTO [abc]([val]) Values('''ABCDE\"')", "Insert Into [abc]([val]) Values('''ABCDE\"')")]

        public async Task ShouldFormattedSqlToTitleCase(string sql, string expectSql)
        {
            var formatter = new DefaultSqlFormatter() { KeywordCase = WordCase.Title };

            var result = await formatter.FormatSql(sql);

            result.Should().Be(expectSql);
        }
    }
}
