using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Descriptors;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeSqlExpressionTranslatorTest : BaseTest
    {
        [Theory]
        [ClassData(typeof(MapToCommonSqlExpression))]
        public async Task ShouldMapToCommonSqlExpression(string sqlExpression, string storeType, SqlExpressionDescriptor sqlExpressionDescriptor)
        {
            var metadataMigrator = SqlCeMetadataMigrator.Default;
            var defaultValue = string.IsNullOrEmpty(sqlExpression) ? string.Empty : $"default {sqlExpression}";
            using var database = CreateDatabase(false, $"create table t1(c1 {storeType} {defaultValue})");
            var context = new MigrationContext
            {
                SourceConnection = database.Connection
            };

            var databaseDesc = await metadataMigrator.GetSourceDatabaseDescriptor(context);
            var tableDesc = databaseDesc.Tables.Single();
            var columnDesc = tableDesc.Columns.Single();
            columnDesc.DefaultValue.Should().BeEquivalentTo(sqlExpressionDescriptor);
        }
        [Theory]
        [ClassData(typeof(MapFromCommonSqlExpression))]
        public async Task ShouldMapFromCommonSqlExpression(SqlExpressionDescriptor sourceSqlExpression, DataTypeDescriptor dataType, string sqlExpression)
        {
            var metadataMigrator = SqlCeMetadataMigrator.Default;

            using var database = CreateDatabase(false);
            var context = new MigrationContext
            {
                SourceConnection = database.Connection,
                TargetConnection = database.Connection,
            };
            var databaseDesc = new DatabaseDescriptor
            {
                Tables = new List<TableDescriptor>
                {
                     new TableDescriptor
                     {
                        Name="t1",
                        Columns=new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor
                            {
                                Name="c1",
                                DataType=dataType,
                                DefaultValue = sourceSqlExpression,
                            }
                        }
                     }
                }
            };
            await metadataMigrator.MigrateAllTargetMetaData(databaseDesc, context);
            var databaseDescFromDB = await metadataMigrator.GetSourceDatabaseDescriptor(context);
            var tableDesc = databaseDescFromDB.Tables.Single();
            var columnDesc = tableDesc.Columns.Single();
            columnDesc.GetOriginDefaultValue().Should().Be(sqlExpression);
        }

        [Theory]
        [InlineData("'\"[]!@#$%^&*()~`\r\n\t", "nvarchar(20)")]
        [InlineData("中\r\n国", "nvarchar(4)")]
        [InlineData("abc", "nvarchar(10)")]
        [InlineData(123, "int")]
        [InlineData(123L, "bigint")]
        [InlineData(123.0, "float")]
        [InlineData(123.0, "double")]
        [InlineData(true, "boolean")]
        [InlineData(false, "boolean")]
        [MemberData(nameof(ConstValues))]
        public async Task ShouldGenerateConstantValueFromDatabase(object constant, string dataType)
        {
            var metadataMigrator = SqlCeMetadataMigrator.Default;

            using var database = CreateDatabase(false);
            var context = new MigrationContext
            {
                SourceConnection = database.Connection,
                TargetConnection = database.Connection,
            };
            var databaseDesc = new DatabaseDescriptor
            {
                Tables = new List<TableDescriptor>
                {
                     new TableDescriptor
                     {
                        Name="t1",
                        Columns=new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor
                            {
                                Name="id",
                                DataType=DataTypeDescriptor.Parse("int"),
                            },
                            new ColumnDescriptor
                            {
                                Name="c1",
                                DataType=DataTypeDescriptor.Parse(dataType),
                                DefaultValue = SqlExpressionDescriptor.FromConstant(constant),
                            }
                        }
                     }
                }
            };
            await metadataMigrator.MigrateAllTargetMetaData(databaseDesc, context);
            database.Connection.ExecuteNonQuery("insert into t1(id) values(1)");
            var generatedValue = database.Connection.ExecuteScalar("select c1 from t1 where id =1");
            generatedValue.Should().BeEquivalentTo(constant);
        }

        public static IEnumerable<object[]> ConstValues()
        {
            yield return new object[] { 123.456M, "decimal(10,3)" };
            yield return new object[] { DateTime.Parse("2022-1-1"), "datetime(6)" };
            yield return new object[] { DateTime.Parse("2022-1-1 12:34:56"), "datetime(6)" };
            yield return new object[] { DateTime.Parse("2022-1-1 12:34:56.1"), "datetime(6)" };
            yield return new object[] { DateTime.Parse("2022-1-1 12:34:56.123"), "datetime(6)" };
            yield return new object[] { new byte[] { 1, 2 }, "varbinary(6)" };
            yield return new object[] { new byte[] { 1, 2, 3, 4, 5 }, "varbinary(5)" };
            yield return new object[] { Guid.NewGuid(), "uuid" };
        }
        class MapFromCommonSqlExpression : List<object[]>
        {
            public MapFromCommonSqlExpression()
            {
                Add(null!, DataTypeDescriptor.Int(), null!);
                Add(SqlExpressionDescriptor.FromConstant(null), DataTypeDescriptor.Int(), "null");
                Add(SqlExpressionDescriptor.FromFunction(Function.Now), DataTypeDescriptor.DateTime(6), "getdate()");
                Add(SqlExpressionDescriptor.FromFunction(Function.Uuid), DataTypeDescriptor.Uuid(), "newid()");
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
                Add(SqlExpressionDescriptor.FromConstant(new byte[] { 1, 15 }), DataTypeDescriptor.Varbinary(10), "0x010F");
                Add(SqlExpressionDescriptor.FromConstant(new byte[] { 1, 15 }), DataTypeDescriptor.Binary(10), "0x010F");
                Add(SqlExpressionDescriptor.FromConstant(new DateTime(2021, 11, 24, 18, 54, 1)), DataTypeDescriptor.DateTime(6), "'2021-11-24 18:54:01'");
                Add(SqlExpressionDescriptor.FromConstant(DateTimeOffset.Parse("2021-11-24 18:54:01.003")), DataTypeDescriptor.DateTimeOffset(6), "'2021-11-24 18:54:01.003'");
            }

            private void Add(SqlExpressionDescriptor descriptor, DataTypeDescriptor dataTypeDescriptor, string targetSqlExpression)
            {

                this.Add(new object[] { descriptor, dataTypeDescriptor, targetSqlExpression == null ? null! : $"({targetSqlExpression})" });
            }
        }

        class MapToCommonSqlExpression : List<object[]>
        {
            public MapToCommonSqlExpression()
            {
                Add(null!, "int", null!);
                Add("", "int", null!);
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
                this.Add(new object[] { sqlExpression, storeType, descriptor });
            }
        }
    }
}
