using System;
using System.Data.Common;
using ChangeDB.Descriptors;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlServerSqlExpressionTranslatorTest
    {
        private readonly ISqlExpressionTranslator sqlTranslator = SqlServerSqlExpressionTranslator.Default;

        private readonly DbConnection _dbConnection;


        public SqlServerSqlExpressionTranslatorTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;

        }

        [Theory]
        [InlineData("(getdate())", Function.Now, null)]
        [InlineData("((GETDATE( )))", Function.Now, null)]
        [InlineData("(newid())", Function.Uuid, null)]
        [InlineData("(((NEWID() )))", Function.Uuid, null)]
        [InlineData("123", null, "123")]
        [InlineData("(123)", null, "123")]
        [InlineData("('abc')", null, "'abc'")]
        [InlineData("null", null, "null")]
        [InlineData("(null)", null, "null")]
        [InlineData("NULL ", null, "NULL")]
        [InlineData("('')", null, "''")]

        public void ShouldMapToCommonSqlExpression(string storedSql, Function? function, string expression)
        {
            sqlTranslator.ToCommonSqlExpression(storedSql, new SqlExpressionTranslatorContext { })
                .Should().BeEquivalentTo(new SqlExpressionDescriptor { Function = function, Expression = expression });
        }
        [Theory]
        [InlineData(Function.Now, null, "getdate()")]
        [InlineData(Function.Uuid, null, "newid()")]
        [InlineData(null, "null", "null")]
        [InlineData(null, "123", "123")]
        [InlineData(null, "''", "''")]
        [InlineData(null, "'abc'", "'abc'")]
        public void ShouldMapFromCommonSqlExpression(Function? function, string expression, string storedSql)
        {
            var sourceSqlExpression = new SqlExpressionDescriptor { Function = function, Expression = expression };
            var targetSqlExpression = sqlTranslator
                 .FromCommonSqlExpression(sourceSqlExpression, new SqlExpressionTranslatorContext { });
            targetSqlExpression.Should().Be(storedSql);
            Action executeExpression = () => _dbConnection.ExecuteScalar($"select {targetSqlExpression}");
            executeExpression.Should().NotThrow();
        }
    }
}
