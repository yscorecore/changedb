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

namespace ChangeDB.Agent.Sqlite
{
    public class SqliteDataTypeMapperTest : BaseTest
    {
        [Theory]
        // SQL Server
        [InlineData("bit", CommonDataType.Boolean, null, null)]
        [InlineData("bit(1)", CommonDataType.Boolean, null, null)]
        [InlineData("bit(2)", CommonDataType.BigInt, null, null)]
        [InlineData("tinyint", CommonDataType.TinyInt, null, null)]
        [InlineData("smallint", CommonDataType.SmallInt, null, null)]
        [InlineData("int", CommonDataType.Int, null, null)]
        [InlineData("bigint", CommonDataType.BigInt, null, null)]
        [InlineData("decimal", CommonDataType.Decimal, 10, 0)]
        [InlineData("decimal(20)", CommonDataType.Decimal, 20, 0)]
        [InlineData("decimal(20,4)", CommonDataType.Decimal, 20, 4)]
        [InlineData("numeric", CommonDataType.Decimal, 0, 0)]
        [InlineData("numeric(20)", CommonDataType.Decimal, 20, 0)]
        [InlineData("numeric(20,4)", CommonDataType.Decimal, 20, 4)]
        [InlineData("smallmoney", CommonDataType.Decimal, 10, 4)]
        [InlineData("money", CommonDataType.Decimal, 19, 4)]
        [InlineData("real", CommonDataType.Double, null, null)]
        [InlineData("float", CommonDataType.Float, null, null)]
        [InlineData("float(24)", CommonDataType.Float, null, null)]
        [InlineData("float(25)", CommonDataType.Float, null, null)]
        [InlineData("float(53)", CommonDataType.Float, null, null)]
        [InlineData("char(10)", CommonDataType.NChar, 10, null)]
        [InlineData("char", CommonDataType.NChar, 1, null)]
        [InlineData("varchar", CommonDataType.Varchar, 1, null)]
        [InlineData("varchar(8000)", CommonDataType.Varchar, 8000, null)]
        [InlineData("nvarchar(4000)", CommonDataType.NVarchar, 4000, null)]
        [InlineData("text", CommonDataType.NText, null, null)]
        [InlineData("ntext", CommonDataType.NText, null, null)]
        [InlineData("xml", CommonDataType.NText, null, null)]

        [InlineData("binary", CommonDataType.Binary, 1, null)]
        [InlineData("binary(10)", CommonDataType.Binary, 10, null)]
        [InlineData("varbinary", CommonDataType.Varbinary, 1, null)]
        [InlineData("varbinary(8000)", CommonDataType.Varbinary, 8000, null)]
        [InlineData("timestamp", CommonDataType.DateTime, 0, null)]
        [InlineData("rowversion", CommonDataType.Binary, 8, null)]
        [InlineData("image", CommonDataType.Blob, null, null)]

        [InlineData("uniqueidentifier", CommonDataType.Uuid, null, null)]

        [InlineData("date", CommonDataType.Date, null, null)]
        [InlineData("time", CommonDataType.Time, 0, null)]
        [InlineData("datetime", CommonDataType.DateTime, 0, null)]
        [InlineData("datetime2", CommonDataType.DateTime, 7, null)]
        [InlineData("datetimeoffset", CommonDataType.DateTimeOffset, 7, null)]

        [InlineData("time(1)", CommonDataType.Time, 1, null)]
        [InlineData("datetime2(1)", CommonDataType.DateTime, 1, null)]
        [InlineData("datetimeoffset(1)", CommonDataType.DateTimeOffset, 1, null)]

        // MySQL
        [InlineData("BOOL", CommonDataType.Boolean, null, null)]
        [InlineData("TINYINT UNSIGNED", CommonDataType.TinyInt, null, null)]
        [InlineData("TINYINT", CommonDataType.TinyInt, null, null)]
        [InlineData("SMALLINT UNSIGNED", CommonDataType.SmallInt, null, null)]
        [InlineData("MEDIUMINT UNSIGNED", CommonDataType.Int, null, null)]
        [InlineData("MEDIUMINT", CommonDataType.Int, null, null)]
        [InlineData("INT UNSIGNED", CommonDataType.Int, null, null)]
        [InlineData("BIGINT UNSIGNED", CommonDataType.BigInt, null, null)]
        [InlineData("YEAR", CommonDataType.Int, null, null)]
        [InlineData("TINYTEXT", CommonDataType.NText, null, null)]
        [InlineData("MEDIUMTEXT", CommonDataType.NText, null, null)]
        [InlineData("LONGTEXT", CommonDataType.NText, null, null)]
        [InlineData("JSON", CommonDataType.NText, null, null)]
        [InlineData("BINARY(16)", CommonDataType.Uuid, null, null)]
        [InlineData("TINYBLOB", CommonDataType.Blob, null, null)]
        [InlineData("MEDIUMBLOB", CommonDataType.Blob, null, null)]
        [InlineData("BLOB", CommonDataType.Blob, null, null)]
        [InlineData("LONGBLOB", CommonDataType.Blob, null, null)]

        // Postgres
        [InlineData("CHARACTER VARYING", CommonDataType.NText, null, null)]
        [InlineData("CHARACTER VARYING(10)", CommonDataType.NVarchar, 10, null)]
        [InlineData("CHARACTER", CommonDataType.NChar, 1, null)]
        [InlineData("DOUBLE PRECISION", CommonDataType.Double, null, null)]
        [InlineData("UUID", CommonDataType.Uuid, null, null)]
        [InlineData("BYTEA", CommonDataType.Blob, null, null)]
        [InlineData("TIMESTAMP WITHOUT TIME ZONE", CommonDataType.DateTime, 6, null)]
        [InlineData("TIMESTAMP WITH TIME ZONE", CommonDataType.DateTimeOffset, 6, null)]
        [InlineData("TIME WITHOUT TIME ZONE", CommonDataType.Time, 6, null)]
        [InlineData("boolean", CommonDataType.Boolean, null, null)]
        public async Task ShouldMapToCommonDataType(string storeType, CommonDataType commonDbType, int? arg1, int? arg2)
        {
            var metadataMigrator = SqliteMetadataMigrator.Default;

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
            var metadataMigrator = SqliteMetadataMigrator.Default;

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
                        Name = "t1",
                        Columns = new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor
                            {
                                Name = "c1",
                                DataType = dataTypeDescriptor
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

                Add(DataTypeDescriptor.Boolean(), "boolean");
                Add(DataTypeDescriptor.TinyInt(), "INTEGER");
                Add(DataTypeDescriptor.SmallInt(), "INTEGER");
                Add(DataTypeDescriptor.Int(), "INTEGER");
                Add(DataTypeDescriptor.BigInt(), "INTEGER");


                Add(DataTypeDescriptor.Uuid(), "uuid");
                Add(DataTypeDescriptor.Text(), "text");
                Add(DataTypeDescriptor.NText(), "ntext");
                Add(DataTypeDescriptor.Blob(), "blob");
                Add(DataTypeDescriptor.Float(), "float");
                Add(DataTypeDescriptor.Double(), "double");
                Add(DataTypeDescriptor.Decimal(20, 4), "decimal(20,4)");

                Add(DataTypeDescriptor.Char(2), "char(2)");
                Add(DataTypeDescriptor.NChar(2), "nchar(2)");
                Add(DataTypeDescriptor.Varchar(2), "varchar(2)");
                Add(DataTypeDescriptor.NVarchar(2), "nvarchar(2)");

                Add(DataTypeDescriptor.Binary(1), "binary(1)");
                Add(DataTypeDescriptor.Varbinary(10), "varbinary(10)");

                Add(DataTypeDescriptor.Date(), "date");
                Add(DataTypeDescriptor.Time(2), "time(2)");
                Add(DataTypeDescriptor.DateTime(2), "datetime(2)");
                Add(DataTypeDescriptor.DateTimeOffset(3), "datetimeoffset(3)");
            }

            private void Add(DataTypeDescriptor descriptor, string targetStoreType)
            {
                this.Add(new object[] { descriptor, targetStoreType });
            }
        }
    }
}
