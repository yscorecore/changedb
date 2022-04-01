using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ChangeDB.Agent.SqlServer;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlCeDataTypeMapperTest : IDisposable
    {
        private readonly IMetadataMigrator _metadataMigrator = new SqlCeAgent().MetadataMigrator;
        private readonly SqlCeDataTypeMapper _dataTypeMapper = SqlCeDataTypeMapper.Default;
        private readonly MigrationContext _migrationContext = new MigrationContext { };
        private readonly DbConnection _dbConnection;

        public SqlCeDataTypeMapperTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;
            _migrationContext = new MigrationContext
            {
                SourceConnection = _dbConnection
            };
        }

        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }

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
            _dbConnection.ExecuteNonQuery($"create table table1(id {storeType});");
            var databaseDescriptor = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var columnStoreType = databaseDescriptor.Tables.SelectMany(p => p.Columns).Select(p => p.GetOriginStoreType()).Single();
            var commonDataType = _dataTypeMapper.ToCommonDatabaseType(columnStoreType);
            var method = typeof(DataTypeDescriptor).GetMethod(Enum.GetName(commonDbType) ?? string.Empty,
                BindingFlags.Static | BindingFlags.Public);
            var parameterLength = method.GetParameters().Length;
            var args = new object[parameterLength];
            if (parameterLength > 0) args[0] = arg1;
            if (parameterLength > 1) args[1] = arg2;
            var typeDescriptor = (DataTypeDescriptor)method.Invoke(null, args);
            commonDataType.Should().BeEquivalentTo(typeDescriptor);
        }

        [Theory]
        [ClassData(typeof(MapToTargetDataTypeTestData))]
        public async Task ShouldMapToTargetDataType(DataTypeDescriptor dataTypeDescriptor, string targetStoreType)
        {
            var targetType = _dataTypeMapper.ToDatabaseStoreType(dataTypeDescriptor);
            _dbConnection.ExecuteNonQuery($"create table table1(id {targetType});");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var targetTypeInDatabase = databaseDesc.Tables.SelectMany(p => p.Columns).Select(p => p.GetOriginStoreType()).First();
            targetTypeInDatabase.Should().Be(targetStoreType);
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
