using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    [Collection(nameof(DatabaseEnvironment))]
    public class PostgresDataTypeMapperTest : IDisposable
    {
        private readonly IDataTypeMapper _dataTypeMapper = PostgresDataTypeMapper.Default;
        private readonly IMetadataMigrator _metadataMigrator = PostgresMetadataMigrator.Default;
        private readonly MigrationContext _migrationSetting = new MigrationContext();
        private readonly DbConnection _dbConnection;

        public PostgresDataTypeMapperTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;
        }
        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }

        [Theory]
        [InlineData("boolean", CommonDataType.Boolean, null, null)]
        [InlineData("varchar(12)", CommonDataType.NVarchar, 12, null)]
        [InlineData("character varying(12)", CommonDataType.NVarchar, 12, null)]
        [InlineData("char(12)", CommonDataType.NChar, 12, null)]
        [InlineData("character(12)", CommonDataType.NChar, 12, null)]
        [InlineData("text", CommonDataType.NText, null, null)]
        [InlineData("varchar", CommonDataType.NText, null, null)]
        [InlineData("char", CommonDataType.NChar, 1, null)]
        [InlineData("int", CommonDataType.Int, null, null)]
        [InlineData("integer", CommonDataType.Int, null, null)]
        [InlineData("smallint", CommonDataType.SmallInt, null, null)]
        [InlineData("bigint", CommonDataType.BigInt, null, null)]

        [InlineData("serial", CommonDataType.Int, null, null)]
        [InlineData("bigserial", CommonDataType.BigInt, null, null)]

        [InlineData("decimal", CommonDataType.Decimal, 38, 4)]
        [InlineData("decimal(3)", CommonDataType.Decimal, 3, 0)]
        [InlineData("decimal(12,3)", CommonDataType.Decimal, 12, 3)]
        [InlineData("dec", CommonDataType.Decimal, 38, 4)]
        [InlineData("dec(3)", CommonDataType.Decimal, 3, 0)]
        [InlineData("dec(12,3)", CommonDataType.Decimal, 12, 3)]

        [InlineData("numeric", CommonDataType.Decimal, 38, 4)]
        [InlineData("numeric(3)", CommonDataType.Decimal, 3, 0)]
        [InlineData("numeric(12,3)", CommonDataType.Decimal, 12, 3)]
        [InlineData("money", CommonDataType.Decimal, 19, 2)]
        [InlineData("double precision", CommonDataType.Double, null, null)]
        [InlineData("float", CommonDataType.Double, null, null)]
        [InlineData("float(1)", CommonDataType.Float, null, null)]
        [InlineData("float(24)", CommonDataType.Float, null, null)]
        [InlineData("float(25)", CommonDataType.Double, null, null)]
        [InlineData("real", CommonDataType.Float, null, null)]
        [InlineData("uuid", CommonDataType.Uuid, null, null)]
        [InlineData("date", CommonDataType.Date, null, null)]
        [InlineData("time", CommonDataType.Time, 6, null)]
        [InlineData("time(4)", CommonDataType.Time, 4, null)]
        [InlineData("time(4) without time zone", CommonDataType.Time, 4, null)]
        [InlineData("timestamp", CommonDataType.DateTime, 6, null)]
        [InlineData("timestamp(3)", CommonDataType.DateTime, 3, null)]
        [InlineData("timestamp without time zone", CommonDataType.DateTime, 6, null)]
        [InlineData("timestamp with time zone", CommonDataType.DateTimeOffset, 6, null)]
        [InlineData("timestamp(1) without time zone", CommonDataType.DateTime, 1, null)]
        [InlineData("timestamp(1) with time zone", CommonDataType.DateTimeOffset, 1, null)]

        public async Task ShouldMapToCommonDataType(string storeType, CommonDataType commonDbType, int? arg1, int? arg2)
        {
            _dbConnection.ExecuteNonQuery($"create table table1(id {storeType});");
            var databaseDescriptor = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            var columnStoreType = databaseDescriptor.Tables.SelectMany(p => p.Columns).Select(p => p.StoreType).Single();
            var commonDataType = _dataTypeMapper.ToCommonDatabaseType(columnStoreType);
            commonDataType.Should().BeEquivalentTo(new DataTypeDescriptor { DbType = commonDbType, Arg1 = arg1, Arg2 = arg2 });

        }
        [Theory]
        [ClassData(typeof(MapToTargetDataTypeTestData))]
        public async Task ShouldMapToTargetDataType(DataTypeDescriptor dataTypeDescriptor, string targetStoreType)
        {
            var targetType = _dataTypeMapper.ToDatabaseStoreType(dataTypeDescriptor);
            _dbConnection.ExecuteNonQuery($"create table table1(id {targetType});");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            var targetTypeInDatabase = databaseDesc.Tables.SelectMany(p => p.Columns).Select(p => p.StoreType).First();
            targetTypeInDatabase.Should().Be(targetStoreType);
        }


        class MapToTargetDataTypeTestData : List<object[]>
        {
            public MapToTargetDataTypeTestData()
            {

                Add(DataTypeDescriptor.Boolean(), "boolean");
                Add(DataTypeDescriptor.TinyInt(), "smallint");
                Add(DataTypeDescriptor.SmallInt(), "smallint");
                Add(DataTypeDescriptor.Int(), "integer");
                Add(DataTypeDescriptor.BigInt(), "bigint");


                Add(DataTypeDescriptor.Uuid(), "uuid");
                Add(DataTypeDescriptor.Text(), "text");
                Add(DataTypeDescriptor.NText(), "text");
                Add(DataTypeDescriptor.Blob(), "bytea");
                Add(DataTypeDescriptor.Float(), "real");
                Add(DataTypeDescriptor.Double(), "double precision");
                Add(DataTypeDescriptor.Decimal(20, 4), "numeric(20,4)");

                Add(DataTypeDescriptor.Char(2), "character(2)");
                Add(DataTypeDescriptor.NChar(2), "character(2)");
                Add(DataTypeDescriptor.Varchar(2), "character varying(2)");
                Add(DataTypeDescriptor.NVarchar(2), "character varying(2)");

                Add(DataTypeDescriptor.Binary(1), "bytea");
                Add(DataTypeDescriptor.Varbinary(10), "bytea");

                Add(DataTypeDescriptor.Date(), "date");
                Add(DataTypeDescriptor.Time(2), "time(2) without time zone");
                Add(DataTypeDescriptor.DateTime(2), "timestamp(2) without time zone");
                Add(DataTypeDescriptor.DateTimeOffset(2), "timestamp(2) with time zone");
            }

            private void Add(DataTypeDescriptor descriptor, string targetStoreType)
            {
                this.Add(new Object[] { descriptor, targetStoreType });
            }
        }
    }
}
