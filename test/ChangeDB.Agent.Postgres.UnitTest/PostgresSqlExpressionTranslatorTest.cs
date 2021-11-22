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
        [InlineData("false", null, "0")]
        [InlineData("true", null, "1")]
        [InlineData("'0001-01-01'::date", null, "'0001-01-01'")]
        [InlineData("'1900-01-01 00:00:00'::timestamp without time zone ", null, "'1900-01-01 00:00:00'")]
        [InlineData("'00:00:00'::time without time zone", null, "'00:00:00'")]
        [InlineData("'00:00:00'::interval", null, "'00:00:00'")]
        [InlineData("'00000000-0000-0000-0000-000000000000'::uuid", null, "'00000000-0000-0000-0000-000000000000'")]
        [InlineData("'0'::numeric", null, "'0'")]
        [InlineData("0.0::numeric(19,4)", null, "0.0")]
        [InlineData("0.1::numeric(19)", null, "0.1")]
        [InlineData("'1900-01-01 00:00:00'::timestamp(3) without time zone ", null, "'1900-01-01 00:00:00'")]

        public void ShouldMapToCommonSqlExpression(string storedSql, Function? function, string expression)
        {
            sqlTranslator.ToCommonSqlExpression(storedSql, new SqlExpressionTranslatorContext { })
                .Should().BeEquivalentTo(new SqlExpressionDescriptor { Function = function, Constant = expression });
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
            var sourceSqlExpression = new SqlExpressionDescriptor { Function = function, Constant = expression };
            var targetSqlExpression = sqlTranslator
                 .FromCommonSqlExpression(sourceSqlExpression, new SqlExpressionTranslatorContext { });
            targetSqlExpression.Should().Be(storedSql);
            Action executeExpression = () => _dbConnection.ExecuteScalar($"select {targetSqlExpression}");
            executeExpression.Should().NotThrow();
        }
    }
}
