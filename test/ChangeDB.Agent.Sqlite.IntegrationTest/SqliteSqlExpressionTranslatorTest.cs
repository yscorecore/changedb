using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Descriptors;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.Sqlite
{
    public class SqliteSqlExpressionTranslatorTest : BaseTest
    {
        [Theory]
        [ClassData(typeof(MapToCommonSqlExpression))]
        public async Task ShouldMapToCommonSqlExpression(string sqlExpression, string storeType, SqlExpressionDescriptor sqlExpressionDescriptor)
        {
            var metadataMigrator = SqliteMetadataMigrator.Default;
            var defaultValue = string.IsNullOrEmpty(sqlExpression) ? string.Empty : $"default {sqlExpression}";
            using var database = CreateDatabase(false, $"create table t1(c1 {storeType} {defaultValue})");
            var context = new AgentContext
            {
                ConnectionString = database.ConnectionString,
                Connection = database.Connection,
            };
            var databaseDesc = await metadataMigrator.GetDatabaseDescriptor(context);
            var tableDesc = databaseDesc.Tables.Single();
            var columnDesc = tableDesc.Columns.Single();
            columnDesc.DefaultValue.Should().BeEquivalentTo(sqlExpressionDescriptor);
        }
        [Theory]
        [ClassData(typeof(MapFromCommonSqlExpression))]
        public async Task ShouldMapFromCommonSqlExpression(SqlExpressionDescriptor sourceSqlExpression, DataTypeDescriptor dataType, string sqlExpression)
        {
            var metadataMigrator = SqliteMetadataMigrator.Default;

            using var database = CreateDatabase(false);
            var context = new AgentContext
            {
                ConnectionString = database.ConnectionString,
                Connection = database.Connection,
            };
            var databaseDesc = new DatabaseDescriptor
            {
                Tables = new List<TableDescriptor>
                {
                     new TableDescriptor
                     {
                        Name = "t1",
                        Columns = new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor
                            {
                                Name = "c1",
                                DataType = dataType,
                                DefaultValue = sourceSqlExpression,
                            }
                        }
                     }
                }
            };
            await metadataMigrator.MigrateAllMetaData(databaseDesc, context);

            var databaseDescFromDB = await metadataMigrator.GetDatabaseDescriptor(context);
            var tableDesc = databaseDescFromDB.Tables.Single();
            var columnDesc = tableDesc.Columns.Single();
            columnDesc.GetOriginDefaultValue().Should().Be(sqlExpression);

        }

        class MapFromCommonSqlExpression : List<object[]>
        {
            public MapFromCommonSqlExpression()
            {
                Add(null!, DataTypeDescriptor.Int(), null!);
                Add(null!, DataTypeDescriptor.Int(), null!);
                Add(SqlExpressionDescriptor.FromConstant(null), DataTypeDescriptor.Int(), null!);
                Add(SqlExpressionDescriptor.FromFunction(Function.Now), DataTypeDescriptor.DateTime(6), "CURRENT_TIMESTAMP");
                Add(SqlExpressionDescriptor.FromFunction(Function.Uuid), DataTypeDescriptor.Uuid(), "randomblob(16)");
                Add(SqlExpressionDescriptor.FromConstant(123), DataTypeDescriptor.Int(), "123");
                Add(SqlExpressionDescriptor.FromConstant(123L), DataTypeDescriptor.BigInt(), "123");
                Add(SqlExpressionDescriptor.FromConstant(true), DataTypeDescriptor.Boolean(), "1");
                Add(SqlExpressionDescriptor.FromConstant(false), DataTypeDescriptor.Boolean(), "0");
                Add(SqlExpressionDescriptor.FromConstant(123.45), DataTypeDescriptor.Double(), "123.45");
                Add(SqlExpressionDescriptor.FromConstant(123.45M), DataTypeDescriptor.Decimal(10, 2), "123.45");
                Add(SqlExpressionDescriptor.FromConstant(""), DataTypeDescriptor.Varchar(10), "''");
                Add(SqlExpressionDescriptor.FromConstant("'"), DataTypeDescriptor.Varchar(10), "''''");
                Add(SqlExpressionDescriptor.FromConstant("''"), DataTypeDescriptor.Varchar(10), "''''''");
                Add(SqlExpressionDescriptor.FromConstant("abc"), DataTypeDescriptor.Varchar(10), "'abc'");
                Add(SqlExpressionDescriptor.FromConstant(""), DataTypeDescriptor.Char(10), "''");
                Add(SqlExpressionDescriptor.FromConstant("'"), DataTypeDescriptor.Char(10), "''''");
                Add(SqlExpressionDescriptor.FromConstant("''"), DataTypeDescriptor.Char(10), "''''''");
                Add(SqlExpressionDescriptor.FromConstant("abc"), DataTypeDescriptor.Char(10), "'abc'");
                Add(SqlExpressionDescriptor.FromConstant(Guid.Empty), DataTypeDescriptor.Uuid(), "'00000000-0000-0000-0000-000000000000'");
                Add(SqlExpressionDescriptor.FromConstant(new byte[] { 1, 15 }), DataTypeDescriptor.Varbinary(10), "x'010F'");
                Add(SqlExpressionDescriptor.FromConstant(new byte[] { 1, 15 }), DataTypeDescriptor.Binary(10), "x'010F'");
                Add(SqlExpressionDescriptor.FromConstant(new DateTime(2021, 11, 24)), DataTypeDescriptor.DateTime(6), "'2021-11-24'");
                Add(SqlExpressionDescriptor.FromConstant(new DateTime(2021, 11, 24, 18, 54, 1)), DataTypeDescriptor.DateTime(6), "'2021-11-24 18:54:01'");
                Add(SqlExpressionDescriptor.FromConstant(new DateTime(2021, 11, 24, 18, 54, 1, 123)), DataTypeDescriptor.DateTime(6), "'2021-11-24 18:54:01.123'");
                Add(SqlExpressionDescriptor.FromConstant(DateTimeOffset.Parse("2021-11-24")), DataTypeDescriptor.DateTimeOffset(6), $"'{DateTimeOffset.Parse("2021-11-24"):yyyy-MM-dd HH:mm:ss zzz}'");
                Add(SqlExpressionDescriptor.FromConstant(DateTimeOffset.Parse("2021-11-24 18:54:01")), DataTypeDescriptor.DateTimeOffset(6), $"'{DateTimeOffset.Parse("2021-11-24 18:54:01"):yyyy-MM-dd HH:mm:ss zzz}'");
                Add(SqlExpressionDescriptor.FromConstant(DateTimeOffset.Parse("2021-11-24 18:54:01.123")), DataTypeDescriptor.DateTimeOffset(6), $"'{DateTimeOffset.Parse("2021-11-24 18:54:01.123"):yyyy-MM-dd HH:mm:ss.fff zzz}'");
                Add(SqlExpressionDescriptor.FromConstant(DateTimeOffset.Parse("2021-11-24 18:54:01 +08")), DataTypeDescriptor.DateTimeOffset(6), $"'{DateTimeOffset.Parse("2021-11-24 18:54:01 +08:00"):yyyy-MM-dd HH:mm:ss zzz}'");
            }

            private void Add(SqlExpressionDescriptor descriptor, DataTypeDescriptor dataType, string targetSqlExpression)
            {
                Add(new object[] { descriptor, dataType, targetSqlExpression == null ? null! : targetSqlExpression });
            }
        }

        class MapToCommonSqlExpression : List<object[]>
        {
            public MapToCommonSqlExpression()
            {
                Add(null!, "int", null!);
                Add("null", "int", null!);
                Add("", "int", null!);
                Add("current_date", "datetime", new SqlExpressionDescriptor() { Function = Function.Now });
                Add("current_time", "datetime", new SqlExpressionDescriptor() { Function = Function.Now });
                Add("current_timestamp", "datetime", new SqlExpressionDescriptor() { Function = Function.Now });
                Add("(randomblob(16))", "uuid", new SqlExpressionDescriptor() { Function = Function.Uuid });
                Add("0", "bit", new SqlExpressionDescriptor() { Constant = false });
                Add("1", "bit", new SqlExpressionDescriptor() { Constant = true });
                Add("123", "int", new SqlExpressionDescriptor() { Constant = 123 });
                Add("(123)", "int", new SqlExpressionDescriptor() { Constant = 123 });
                Add("(123)", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("(123)", "bigint", new SqlExpressionDescriptor() { Constant = 123L });
                Add("(123.45)", "decimal(10,2)", new SqlExpressionDescriptor() { Constant = 123.45m });
                Add("(123.45)", "float", new SqlExpressionDescriptor() { Constant = 123.45f });
                Add("null", "nvarchar(10)", null!);
                Add("null", "bigint", null!);
                Add("('123')", "int", new SqlExpressionDescriptor() { Constant = 123 });
                Add("('123')", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("('')", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "" });
                Add("('''')", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "'" });
                Add("('''''')", "nvarchar(10)", new SqlExpressionDescriptor() { Constant = "''" });

                Add("('123')", "nchar(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("('')", "nchar(10)", new SqlExpressionDescriptor() { Constant = "" });
                Add("('''')", "nchar(10)", new SqlExpressionDescriptor() { Constant = "'" });
                Add("('''''')", "nchar(10)", new SqlExpressionDescriptor() { Constant = "''" });

                Add("('123')", "varchar(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("('')", "varchar(10)", new SqlExpressionDescriptor() { Constant = "" });
                Add("('''')", "varchar(10)", new SqlExpressionDescriptor() { Constant = "'" });
                Add("('''''')", "varchar(10)", new SqlExpressionDescriptor() { Constant = "''" });

                Add("('123')", "char(10)", new SqlExpressionDescriptor() { Constant = "123" });
                Add("('')", "char(10)", new SqlExpressionDescriptor() { Constant = "" });
                Add("('''')", "char(10)", new SqlExpressionDescriptor() { Constant = "'" });
                Add("('''''')", "char(10)", new SqlExpressionDescriptor() { Constant = "''" });

                Add("('00000000-0000-0000-0000-000000000000')", "uniqueidentifier", new SqlExpressionDescriptor() { Constant = Guid.Empty });
                Add("x'1122'", "varbinary(5)", new SqlExpressionDescriptor() { Constant = new byte[] { 0x11, 0x22 } });
                Add("x'1122'", "binary(5)", new SqlExpressionDescriptor() { Constant = new byte[] { 0x11, 0x22 } });
                Add("'2021-11-24 18:54:01'", "datetime2", new SqlExpressionDescriptor() { Constant = new DateTime(2021, 11, 24, 18, 54, 1) });
                Add("'2021-11-24 18:54:01 +08:00'", "datetimeoffset", new SqlExpressionDescriptor() { Constant = DateTimeOffset.Parse("2021-11-24 18:54:01 +08:00") });
            }
            private void Add(string sqlExpression, string storeType, SqlExpressionDescriptor descriptor)
            {
                this.Add(new Object[] { sqlExpression, storeType, descriptor });
            }
        }
    }
}
