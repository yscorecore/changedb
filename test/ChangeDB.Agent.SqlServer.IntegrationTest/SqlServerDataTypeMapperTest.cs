using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using TestDB;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDataTypeMapperTest : BaseTest
    {


        [Theory]
        [InlineData("bit", CommonDataType.Boolean, null, null)]
        [InlineData("tinyint", CommonDataType.TinyInt, null, null)]
        [InlineData("smallint", CommonDataType.SmallInt, null, null)]
        [InlineData("int", CommonDataType.Int, null, null)]
        [InlineData("bigint", CommonDataType.BigInt, null, null)]
        [InlineData("decimal", CommonDataType.Decimal, 18, 0)]
        [InlineData("decimal(20)", CommonDataType.Decimal, 20, 0)]
        [InlineData("decimal(20,4)", CommonDataType.Decimal, 20, 4)]
        [InlineData("numeric", CommonDataType.Decimal, 18, 0)]
        [InlineData("numeric(20)", CommonDataType.Decimal, 20, 0)]
        [InlineData("numeric(20,4)", CommonDataType.Decimal, 20, 4)]
        [InlineData("smallmoney", CommonDataType.Decimal, 10, 4)]
        [InlineData("money", CommonDataType.Decimal, 19, 4)]
        [InlineData("real", CommonDataType.Float, null, null)]
        [InlineData("float", CommonDataType.Double, null, null)]
        [InlineData("float(24)", CommonDataType.Float, null, null)]
        [InlineData("float(25)", CommonDataType.Double, null, null)]
        [InlineData("float(53)", CommonDataType.Double, null, null)]
        [InlineData("char(10)", CommonDataType.Char, 10, null)]
        [InlineData("char", CommonDataType.Char, 1, null)]
        [InlineData("varchar", CommonDataType.Varchar, 1, null)]
        [InlineData("varchar(8000)", CommonDataType.Varchar, 8000, null)]
        [InlineData("varchar(MAX)", CommonDataType.Text, null, null)]
        [InlineData("nvarchar(4000)", CommonDataType.NVarchar, 4000, null)]
        [InlineData("nvarchar(MAX)", CommonDataType.NText, null, null)]
        [InlineData("text", CommonDataType.Text, null, null)]
        [InlineData("ntext", CommonDataType.NText, null, null)]
        [InlineData("xml", CommonDataType.NText, null, null)]

        [InlineData("binary", CommonDataType.Binary, 1, null)]
        [InlineData("binary(10)", CommonDataType.Binary, 10, null)]
        [InlineData("varbinary", CommonDataType.Varbinary, 1, null)]
        [InlineData("varbinary(8000)", CommonDataType.Varbinary, 8000, null)]
        [InlineData("varbinary(MAX)", CommonDataType.Blob, null, null)]
        [InlineData("timestamp", CommonDataType.Binary, 8, null)]
        [InlineData("rowversion", CommonDataType.Binary, 8, null)]
        [InlineData("image", CommonDataType.Blob, null, null)]

        [InlineData("uniqueidentifier", CommonDataType.Uuid, null, null)]

        [InlineData("date", CommonDataType.Date, null, null)]
        [InlineData("time", CommonDataType.Time, 7, null)]
        [InlineData("datetime", CommonDataType.DateTime, 3, null)]
        [InlineData("datetime2", CommonDataType.DateTime, 7, null)]
        [InlineData("datetimeoffset", CommonDataType.DateTimeOffset, 7, null)]

        [InlineData("time(1)", CommonDataType.Time, 1, null)]
        [InlineData("datetime2(1)", CommonDataType.DateTime, 1, null)]
        [InlineData("datetimeoffset(1)", CommonDataType.DateTimeOffset, 1, null)]
        public async Task ShouldMapToCommonDataType(string storeType, CommonDataType commonDbType, int? arg1, int? arg2)
        {
            var metadataMigrator = SqlServerMetadataMigrator.Default;

            using var database = CreateDatabase(false, $"create table t1(c1 {storeType})");
            var context = new AgentContext
            {
                ConnectionString = database.ConnectionString,
                Connection = database.Connection
            };

            var databaseDesc = await metadataMigrator.GetDatabaseDescriptor(context);
            var tableDesc = databaseDesc.Tables.Single();
            var columnDesc = tableDesc.Columns.Single();
            columnDesc.DataType.Should().BeEquivalentTo(new DataTypeDescriptor
            {
                DbType = commonDbType,
                Arg1 = arg1,
                Arg2 = arg2
            });
        }

        [Theory]
        [ClassData(typeof(MapToTargetDataTypeTestData))]
        public async Task ShouldMapToTargetDataType(DataTypeDescriptor dataTypeDescriptor, string targetStoreType)
        {
            var metadataMigrator = SqlServerMetadataMigrator.Default;

            using var database = CreateDatabase(false);
            var context = new AgentContext
            {
                Connection = database.Connection,
                ConnectionString = database.ConnectionString
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
                                DataType=dataTypeDescriptor
                            }
                        }
                     }
                }
            };
            await metadataMigrator.MigrateAllMetaData(databaseDesc, context);

            var databaseDescFromDB = await metadataMigrator.GetDatabaseDescriptor(context);
            databaseDescFromDB.Tables.Single().Columns.Single().GetOriginStoreType().Should().Be(targetStoreType);
        }

        class MapToTargetDataTypeTestData : List<object[]>
        {
            public MapToTargetDataTypeTestData()
            {

                Add(DataTypeDescriptor.Boolean(), "bit");
                Add(DataTypeDescriptor.TinyInt(), "tinyint");
                Add(DataTypeDescriptor.SmallInt(), "smallint");
                Add(DataTypeDescriptor.Int(), "int");
                Add(DataTypeDescriptor.BigInt(), "bigint");


                Add(DataTypeDescriptor.Uuid(), "uniqueidentifier");
                Add(DataTypeDescriptor.Text(), "text");
                Add(DataTypeDescriptor.NText(), "ntext");
                Add(DataTypeDescriptor.Blob(), "image");
                Add(DataTypeDescriptor.Float(), "real");
                Add(DataTypeDescriptor.Double(), "float");
                Add(DataTypeDescriptor.Decimal(20, 4), "decimal(20, 4)");

                Add(DataTypeDescriptor.Char(2), "char(2)");
                Add(DataTypeDescriptor.NChar(2), "nchar(2)");
                Add(DataTypeDescriptor.Varchar(2), "varchar(2)");
                Add(DataTypeDescriptor.NVarchar(2), "nvarchar(2)");

                Add(DataTypeDescriptor.Binary(1), "binary(1)");
                Add(DataTypeDescriptor.Varbinary(10), "varbinary(10)");

                Add(DataTypeDescriptor.Date(), "date");
                Add(DataTypeDescriptor.Time(2), "time(2)");
                Add(DataTypeDescriptor.DateTime(2), "datetime2(2)");
                Add(DataTypeDescriptor.DateTimeOffset(3), "datetimeoffset(3)");
            }

            private void Add(DataTypeDescriptor descriptor, string targetStoreType)
            {
                this.Add(new object[] { descriptor, targetStoreType });
            }
        }
    }
}
