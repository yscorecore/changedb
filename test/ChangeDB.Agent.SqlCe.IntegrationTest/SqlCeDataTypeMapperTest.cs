using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlCeDataTypeMapperTest : BaseTest
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

        [InlineData("money", CommonDataType.Decimal, 19, 4)]
        [InlineData("real", CommonDataType.Float, null, null)]
        [InlineData("float", CommonDataType.Double, null, null)]
        [InlineData("nchar(10)", CommonDataType.NChar, 10, null)]
        [InlineData("nvarchar(4000)", CommonDataType.NVarchar, 4000, null)]
        [InlineData("ntext", CommonDataType.NText, null, null)]
        [InlineData("binary", CommonDataType.Binary, 1, null)]
        [InlineData("binary(10)", CommonDataType.Binary, 10, null)]
        [InlineData("varbinary", CommonDataType.Varbinary, 1, null)]
        [InlineData("varbinary(8000)", CommonDataType.Varbinary, 8000, null)]
        [InlineData("timestamp", CommonDataType.Binary, 8, null)]
        [InlineData("rowversion", CommonDataType.Binary, 8, null)]
        [InlineData("image", CommonDataType.Blob, null, null)]
        [InlineData("uniqueidentifier", CommonDataType.Uuid, null, null)]

        [InlineData("datetime", CommonDataType.DateTime, 3, null)]

        public async Task ShouldMapToCommonDataType(string storeType, CommonDataType commonDbType, int? arg1, int? arg2)
        {
            var metadataMigrator = SqlCeMetadataMigrator.Default;

            using var database = CreateDatabase(false, $"create table t1(c1 {storeType})");
            var context = new AgentContext
            {
                Connection = database.Connection,
                ConnectionString = database.ConnectionString
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
            var metadataMigrator = SqlCeMetadataMigrator.Default;

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
                Add(DataTypeDescriptor.Text(), "ntext");
                Add(DataTypeDescriptor.NText(), "ntext");
                Add(DataTypeDescriptor.Blob(), "image");
                Add(DataTypeDescriptor.Float(), "real");
                Add(DataTypeDescriptor.Double(), "float");
                Add(DataTypeDescriptor.Decimal(20, 4), "numeric(20, 4)");

                Add(DataTypeDescriptor.Char(2), "nchar(2)");
                Add(DataTypeDescriptor.NChar(2), "nchar(2)");
                Add(DataTypeDescriptor.Varchar(2), "nvarchar(2)");
                Add(DataTypeDescriptor.NVarchar(2), "nvarchar(2)");

                Add(DataTypeDescriptor.Binary(1), "binary(1)");
                Add(DataTypeDescriptor.Varbinary(10), "varbinary(10)");

                Add(DataTypeDescriptor.Date(), "datetime");
                Add(DataTypeDescriptor.Time(2), "datetime");
                Add(DataTypeDescriptor.DateTime(2), "datetime");
                Add(DataTypeDescriptor.DateTimeOffset(2), "datetime");
            }

            private void Add(DataTypeDescriptor descriptor, string targetStoreType)
            {
                this.Add(new Object[] { descriptor, targetStoreType });
            }
        }
    }
}
