using System;
using System.Collections.Generic;
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




        [ClassData(typeof(MapToCommonSqlExpression))]

        public void ShouldMapToCommonSqlExpression(string sqlExpression, string storeType, SqlExpressionDescriptor sqlExpressionDescriptor)
        {
            sqlTranslator.ToCommonSqlExpression(sqlExpression, new SqlExpressionTranslatorContext { StoreType = storeType, Connection = _dbConnection })
                .Should().BeEquivalentTo(sqlExpressionDescriptor);
        }
        [Theory]


        [ClassData(typeof(MapFromCommonSqlExpression))]
        public void ShouldMapFromCommonSqlExpression(SqlExpressionDescriptor sourceSqlExpression, string storeType, string sqlExpression)
        {
            var targetSqlExpression = sqlTranslator
                .FromCommonSqlExpression(sourceSqlExpression, new SqlExpressionTranslatorContext { StoreType = storeType, Connection = _dbConnection });
            targetSqlExpression.Should().Be(sqlExpression);
            Action executeExpression = () => _dbConnection.ExecuteScalar($"select {targetSqlExpression}");
            executeExpression.Should().NotThrow();
        }

        class MapFromCommonSqlExpression : List<object[]>
        {

            public MapFromCommonSqlExpression()
            {
                Add(null, "integer", "null");
                Add(new SqlExpressionDescriptor(), "integer", "null");
                Add(new SqlExpressionDescriptor { Function = Function.Now }, "timestamp", "now()");
                Add(new SqlExpressionDescriptor { Function = Function.Uuid }, "uuid", "gen_random_uuid()");
                Add(new SqlExpressionDescriptor { Constant = null }, "integer", "null");
                Add(new SqlExpressionDescriptor { Constant = 123 }, "integer", "123");
                Add(new SqlExpressionDescriptor { Constant = 123L }, "bigint", "123");
                Add(new SqlExpressionDescriptor { Constant = true }, "boolean", "true");
                Add(new SqlExpressionDescriptor { Constant = false }, "boolean", "false");
                Add(new SqlExpressionDescriptor { Constant = 1 }, "boolean", "true");
                Add(new SqlExpressionDescriptor { Constant = 0 }, "boolean", "false");
                Add(new SqlExpressionDescriptor { Constant = 123.45 }, "float", "123.45");
                Add(new SqlExpressionDescriptor { Constant = 123.45M }, "decimal", "123.45");
                Add(new SqlExpressionDescriptor { Constant = "" }, "varchar(10)", "''");
                Add(new SqlExpressionDescriptor { Constant = "'" }, "varchar(10)", "''''");
                Add(new SqlExpressionDescriptor { Constant = "''" }, "varchar(10)", "''''''");
                Add(new SqlExpressionDescriptor { Constant = "\r\n\t" }, "varchar(10)", @"E'\r\n\t'");
                Add(new SqlExpressionDescriptor { Constant = "abc" }, "varchar(10)", "'abc'");
                Add(new SqlExpressionDescriptor { Constant = Guid.Empty }, "uuid", "'00000000-0000-0000-0000-000000000000'::uuid");
                Add(new SqlExpressionDescriptor { Constant = new byte[] { 1, 15 } }, "bytea", "'\\x010F'::bytea");
                Add(new SqlExpressionDescriptor { Constant = new DateTime(2021, 11, 24, 18, 54, 1) }, "timestamp(6) without time zone", "'2021-11-24 18:54:01'::timestamp(6) without time zone");
                Add(new SqlExpressionDescriptor { Constant = DateTimeOffset.Parse("2021-11-24 18:54:01 +08") }, "timestamp(6) with time zone", "'2021-11-24 18:54:01 +08:00'::timestamp(6) with time zone");

            }

            private void Add(SqlExpressionDescriptor descriptor, string storeType, string targetSqlExpression)
            {
                this.Add(new Object[] { descriptor, storeType, targetSqlExpression });
            }
        }

        class MapToCommonSqlExpression : List<object[]>
        {

            public MapToCommonSqlExpression()
            {
                Add(null, "integer", null);
                Add("", "integer", null);
                Add("current_timestamp", "timestamp", new SqlExpressionDescriptor() { Function = Function.Now });
                Add("current_timestamp(1)", "timestamp", new SqlExpressionDescriptor() { Function = Function.Now });
                Add("current_timestamp(6)", "timestamp", new SqlExpressionDescriptor() { Function = Function.Now });
                Add("gen_random_uuid()", "uuid", new SqlExpressionDescriptor() { Function = Function.Uuid });


                Add("0", "boolean", new SqlExpressionDescriptor() { Constant = false });
                Add("1", "boolean", new SqlExpressionDescriptor() { Constant = true });
                Add("false", "boolean", new SqlExpressionDescriptor() { Constant = false });
                Add("true", "boolean", new SqlExpressionDescriptor() { Constant = true });

                Add("123", "integer", new SqlExpressionDescriptor() { Constant = 123 });
                Add("123", "bigint", new SqlExpressionDescriptor() { Constant = 123L });
                Add("123", "varchar(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("123.45", "decimal(10,2)", new SqlExpressionDescriptor() { Constant = 123.45m });
                Add("123.45", "float", new SqlExpressionDescriptor() { Constant = 123.45d });
                Add("null", "varchar(10)", new SqlExpressionDescriptor());
                Add("null", "bigint", new SqlExpressionDescriptor());
                Add("'123'", "varchar(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("'123'", "integer", new SqlExpressionDescriptor() { Constant = 123 });
                Add("''", "varchar(10)", new SqlExpressionDescriptor() { Constant = "" });
                Add("''''", "varchar(10)", new SqlExpressionDescriptor() { Constant = "'" });
                Add("''''''", "varchar(10)", new SqlExpressionDescriptor() { Constant = "''" });
                Add("'00000000-0000-0000-0000-000000000000'", "uuid", new SqlExpressionDescriptor() { Constant = Guid.Empty });
                Add("'\\x1122'", "bytea", new SqlExpressionDescriptor() { Constant = new Byte[] { 0x11, 0x22 } });
                Add("'2021-11-24 18:54:01'", "timestamp without time zone", new SqlExpressionDescriptor() { Constant = new DateTime(2021, 11, 24, 18, 54, 1) });


                Add("'0001-01-01'", "date", new SqlExpressionDescriptor() { Constant = new DateTime(1, 1, 1) });
                Add("'1900-01-01 00:00:00'", "timestamp without time zone ", new SqlExpressionDescriptor() { Constant = new DateTime(1900, 1, 1) });



                Add("'0'::numeric", "numeric", new SqlExpressionDescriptor() { Constant = 0.0 });

                Add("0.0::numeric(19,4)", "numeric", new SqlExpressionDescriptor() { Constant = 0.0M });

                Add("0.0::numeric(19,4)", "numeric", new SqlExpressionDescriptor() { Constant = 0.0M });
                Add("'1900-01-01 00:00:00'::timestamp(3) without time zone ", "timestamp without time zone ", new SqlExpressionDescriptor() { Constant = new DateTime(1900, 1, 1) });


            }
            private void Add(string sqlExpression, string storeType, SqlExpressionDescriptor descriptor)
            {
                this.Add(new Object[] { sqlExpression, storeType, descriptor });
            }
        }
    }
}
