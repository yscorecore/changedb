using System;
using System.Data;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChangeDB.Core.Utils
{
    public class ConnectionExtensionsTest
    {
        [Fact]
        public void ShouldNotInvokeWhenExecuteSqlScriptAndSqlScriptIsEmpty()
        {
            var dbCommand = Mock.Of<IDbCommand>();
            var dbConnection = Mock.Of<IDbConnection>(p => p.CreateCommand() == dbCommand);
            dbConnection.ExecuteSqlScript(string.Empty, String.Empty);
            Mock.Get(dbCommand).Verify(p => p.ExecuteNonQuery(), Times.Never);
        }
        [Fact]
        public void ShouldInvokeOnceWhenExecuteSqlScriptAndSqlScriptIsOneLine()
        {
            var dbCommand = Mock.Of<IDbCommand>();
            var dbConnection = Mock.Of<IDbConnection>(p => p.CreateCommand() == dbCommand);
            dbConnection.ExecuteSqlScript("line1", String.Empty);
            Mock.Get(dbCommand).Verify(p => p.ExecuteNonQuery(), Times.Once);
        }
        [Fact]
        public void ShouldInvokeTwiceWhenExecuteSqlScriptAndSqlScriptIsMultiLinesAndSplitIsEmpty()
        {
            var dbCommand = Mock.Of<IDbCommand>();
            var dbConnection = Mock.Of<IDbConnection>(p => p.CreateCommand() == dbCommand);
            dbConnection.ExecuteSqlScript("line1\nline2\n\nline3", String.Empty);
            Mock.Get(dbCommand).Verify(p => p.ExecuteNonQuery(), Times.Exactly(2));
            dbCommand.CommandText.Should().Be("line3;");
        }
        [Fact]
        public void ShouldInvokeTwiceWhenExecuteSqlScriptAndSqlScriptIsMultiLinesAndSplitIsNotEmpty()
        {
            var dbCommand = Mock.Of<IDbCommand>();
            var dbConnection = Mock.Of<IDbConnection>(p => p.CreateCommand() == dbCommand);
            dbConnection.ExecuteSqlScript("line1\ngo\nline3", "go");
            Mock.Get(dbCommand).Verify(p => p.ExecuteNonQuery(), Times.Exactly(2));
            dbCommand.CommandText.Should().Be("line3;");
        }

        [Fact]
        public void ShouldCallbackWhenExecuteSqlScriptAndSqlScriptIsMultiLinesAndSplitIsNotEmpty()
        {
            var dbCommand = Mock.Of<IDbCommand>(p => p.ExecuteNonQuery() == 1);
            var dbConnection = Mock.Of<IDbConnection>(p => p.CreateCommand() == dbCommand);
            var action = Mock.Of<Action<(int StartLine, int LineCount, string Sql, int Result)>>();
            dbConnection.ExecuteSqlScript("line1\nline2\ngo\nline3", "go", action);
            var first = (StartLine: 1, LineCount: 2, Sql: "line1\nline2", Result: 1);
            Mock.Get(action).Verify(p => p.Invoke(first), Times.Once);
            var second = (StartLine: 4, LineCount: 1, Sql: "line3", Result: 1);
            Mock.Get(action).Verify(p => p.Invoke(second), Times.Once);
        }
    }
}
