using System;
using System.Collections.Generic;
using System.Data.Common;
using ChangeDB.Agent.SqlServer;
using ChangeDB.Descriptors;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlCeSqlExpressionTranslatorTest
    {
        private readonly SqlCeSqlExpressionTranslator sqlTranslator = SqlCeSqlExpressionTranslator.Default;

        private readonly DbConnection _dbConnection;


        public SqlCeSqlExpressionTranslatorTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;

        }

        [Theory]
        [ClassData(typeof(MapToCommonSqlExpression))]
        [Obsolete]
        public void ShouldMapToCommonSqlExpression(string sqlExpression, string storeType, SqlExpressionDescriptor sqlExpressionDescriptor)
        {
            sqlTranslator.ToCommonSqlExpression(sqlExpression, storeType, _dbConnection)
                .Should().BeEquivalentTo(sqlExpressionDescriptor);
        }
        [Theory]
        [ClassData(typeof(MapFromCommonSqlExpression))]
        public void ShouldMapFromCommonSqlExpression(SqlExpressionDescriptor sourceSqlExpression, string sqlExpression, string storeType)
        {
            var targetSqlExpression = sqlTranslator
                 .FromCommonSqlExpression(sourceSqlExpression, storeType);
            targetSqlExpression.Should().Be(sqlExpression);
            Action executeExpression = () => _dbConnection.ExecuteScalar($"select {targetSqlExpression}");
            executeExpression.Should().NotThrow();
        }

        class MapFromCommonSqlExpression : List<object[]>
        {
            public MapFromCommonSqlExpression()
            {
                Add(null!, "null");
                Add(new SqlExpressionDescriptor(), "null");
                Add(new SqlExpressionDescriptor { Function = Function.Now }, "getdate()");
                Add(new SqlExpressionDescriptor { Function = Function.Uuid }, "newid()");
                Add(new SqlExpressionDescriptor { Constant = null }, "null");
                Add(new SqlExpressionDescriptor { Constant = 123 }, "123");
                Add(new SqlExpressionDescriptor { Constant = 123L }, "123");
                Add(new SqlExpressionDescriptor { Constant = true }, "1");
                Add(new SqlExpressionDescriptor { Constant = false }, "0");
                Add(new SqlExpressionDescriptor { Constant = 123.45 }, "123.45");
                Add(new SqlExpressionDescriptor { Constant = 123.45M }, "123.45");
                Add(new SqlExpressionDescriptor { Constant = "" }, "''");
                Add(new SqlExpressionDescriptor { Constant = "'" }, "''''");
                Add(new SqlExpressionDescriptor { Constant = "''" }, "''''''");
                Add(new SqlExpressionDescriptor { Constant = "abc" }, "'abc'");
                Add(new SqlExpressionDescriptor { Constant = Guid.Empty }, "'00000000-0000-0000-0000-000000000000'");
                Add(new SqlExpressionDescriptor { Constant = new byte[] { 1, 15 } }, "0x010F");
                Add(new SqlExpressionDescriptor { Constant = new DateTime(2021, 11, 24, 18, 54, 1) }, "'2021-11-24 18:54:01'");
                Add(new SqlExpressionDescriptor { Constant = DateTimeOffset.Parse("2021-11-24 18:54:01 +08") }, "'2021-11-24 18:54:01 +08:00'");


            }

            private void Add(SqlExpressionDescriptor descriptor, string targetSqlExpression)
            {
                this.Add(new Object[] { descriptor, targetSqlExpression });
            }
        }

        class MapToCommonSqlExpression : List<object[]>
        {
            public MapToCommonSqlExpression()
            {
                Add(null!, "int", null!);
                Add("", "int", null!);
                Add("(())", "int", null!);
                Add("(getdate())", "datetime", new SqlExpressionDescriptor() { Function = Function.Now });
                Add("((GETDATE( )))", "datetime", new SqlExpressionDescriptor() { Function = Function.Now });
                Add("(newid())", "uniqueidentifier", new SqlExpressionDescriptor() { Function = Function.Uuid });
                Add("(((NEWID() )))", "uniqueidentifier", new SqlExpressionDescriptor() { Function = Function.Uuid });
                Add("0", "bit", new SqlExpressionDescriptor() { Constant = false });
                Add("1", "bit", new SqlExpressionDescriptor() { Constant = true });
                Add("123", "int", new SqlExpressionDescriptor() { Constant = 123 });
                Add("(123)", "int", new SqlExpressionDescriptor() { Constant = 123 });
                Add("(123)", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("(123)", "bigint", new SqlExpressionDescriptor() { Constant = 123L });
                Add("(123.45)", "decimal(10,2)", new SqlExpressionDescriptor() { Constant = 123.45m });
                Add("(123.45)", "float", new SqlExpressionDescriptor() { Constant = 123.45d });
                Add("null", "nvarchar(10)", new SqlExpressionDescriptor());
                Add("null", "bigint", new SqlExpressionDescriptor());
                Add("('123')", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("('123')", "int", new SqlExpressionDescriptor() { Constant = 123 });
                Add("('')", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "" });
                Add("('''')", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "'" });
                Add("('''''')", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "''" });
                Add("('00000000-0000-0000-0000-000000000000')", "uniqueidentifier", new SqlExpressionDescriptor() { Constant = Guid.Empty });
                Add("0", "varbinary(5)", new SqlExpressionDescriptor() { Constant = new Byte[4] });
                Add("0x1122", "varbinary(5)", new SqlExpressionDescriptor() { Constant = new Byte[] { 0x11, 0x22 } });
                Add("0x1122", "binary(5)", new SqlExpressionDescriptor() { Constant = new Byte[] { 0x11, 0x22, 0x00, 0x00, 0x00 } });
                Add("'2021-11-24 18:54:01'", "datetime", new SqlExpressionDescriptor() { Constant = new DateTime(2021, 11, 24, 18, 54, 1) });

            }
            private void Add(string sqlExpression, string storeType, SqlExpressionDescriptor descriptor)
            {
                this.Add(new Object[] { sqlExpression, storeType, descriptor });
            }
        }
    }
}
