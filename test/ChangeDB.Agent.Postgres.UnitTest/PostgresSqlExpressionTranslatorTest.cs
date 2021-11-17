using System;
using System.Data.Common;
using ChangeDB.Descriptors;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    [Collection(nameof(DatabaseEnvironment))]
    public class PostgresSqlExpressionTranslatorTest
    {
        private readonly ISqlExpressionTranslator sqlTranslator = PostgresSqlExpressionTranslator.Default;
        private readonly DbConnection _dbConnection;


        public PostgresSqlExpressionTranslatorTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;

        }
        [Theory]
        [InlineData("current_timestamp", Function.Now, null)]
        [InlineData("current_timestamp(1)", Function.Now, null)]
        [InlineData("current_timestamp(6)", Function.Now, null)]
        [InlineData("gen_random_uuid()", Function.Uuid, null)]
        [InlineData("123", null, "123")]
        [InlineData("null", null, "null")]
        [InlineData("NULL ", null, "NULL")]
        [InlineData("''", null, "''")]

        public void ShouldMapToCommonSqlExpression(string storedSql, Function? function, string expression)
        {
            sqlTranslator.ToCommonSqlExpression(storedSql)
                .Should().BeEquivalentTo(new SqlExpressionDescriptor { Function = function, Expression = expression });
        }
        [Theory]
        [InlineData(Function.Now, null, "now()")]
        [InlineData(Function.Uuid, null, "gen_random_uuid()")]
        [InlineData(null, "null", "null")]
        [InlineData(null, "123", "123")]
        [InlineData(null, "''", "''")]
        [InlineData(null, "'abc'", "'abc'")]
        public void ShouldMapFromCommonSqlExpression(Function? function, string expression, string storedSql)
        {
            var targetSqlExpression = sqlTranslator
                 .FromCommonSqlExpression(new SqlExpressionDescriptor { Function = function, Expression = expression });
            targetSqlExpression.Should().Be(storedSql);
            Action executeExpression = () => _dbConnection.ExecuteScalar($"select {targetSqlExpression}");
            executeExpression.Should().NotThrow();
        }
    }
}
