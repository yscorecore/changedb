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
    public class PostgresDatabaseTypeMapperTest:IDisposable
    {
        private readonly IDatabaseTypeMapper _databaseTypeMapper = PostgresDatabaseTypeMapper.Default;
        private readonly IMetadataMigrator _metadataMigrator = PostgresMetadataMigrator.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting();
        private readonly DbConnection _dbConnection;

        public PostgresDatabaseTypeMapperTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;
        }
        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }
        
        [Theory]
        [InlineData("varchar(12)", CommonDatabaseType.NVarchar, 12, null)]
        [InlineData("character varying(12)", CommonDatabaseType.NVarchar, 12, null)]
        [InlineData("char(12)", CommonDatabaseType.NChar, 12, null)]
        [InlineData("character(12)", CommonDatabaseType.NChar, 12, null)]
        [InlineData("text", CommonDatabaseType.NText, null, null)]
        [InlineData("varchar", CommonDatabaseType.NText, null, null)]
        [InlineData("char", CommonDatabaseType.NChar, 1, null)]
        [InlineData("int", CommonDatabaseType.Int, null, null)]
        [InlineData("integer", CommonDatabaseType.Int, null, null)]
        [InlineData("smallint", CommonDatabaseType.SmallInt, null, null)]
        [InlineData("bigint", CommonDatabaseType.BigInt, null, null)]

        [InlineData("serial", CommonDatabaseType.Int, null, null)]
        [InlineData("bigserial", CommonDatabaseType.BigInt, null, null)]

        [InlineData("decimal", CommonDatabaseType.Decimal, 38, 4)]
        [InlineData("decimal(3)", CommonDatabaseType.Decimal, 3, 0)]
        [InlineData("decimal(12,3)", CommonDatabaseType.Decimal, 12, 3)]
        [InlineData("dec", CommonDatabaseType.Decimal, 38, 4)]
        [InlineData("dec(3)", CommonDatabaseType.Decimal, 3, 0)]
        [InlineData("dec(12,3)", CommonDatabaseType.Decimal, 12, 3)]

        [InlineData("numeric", CommonDatabaseType.Decimal, 38, 4)]
        [InlineData("numeric(3)", CommonDatabaseType.Decimal, 3, 0)]
        [InlineData("numeric(12,3)", CommonDatabaseType.Decimal, 12, 3)]
        [InlineData("money", CommonDatabaseType.Decimal, 19, 2)]
        [InlineData("double precision", CommonDatabaseType.Double, null, null)]
        [InlineData("float", CommonDatabaseType.Double, null, null)]
        [InlineData("float(1)", CommonDatabaseType.Float, null, null)]
        [InlineData("float(24)", CommonDatabaseType.Float, null, null)]
        [InlineData("float(25)", CommonDatabaseType.Double, null, null)]
        [InlineData("real", CommonDatabaseType.Float, null, null)]
        [InlineData("uuid", CommonDatabaseType.Uuid, null, null)]
        [InlineData("date", CommonDatabaseType.Date, null, null)]
        [InlineData("time", CommonDatabaseType.Time, 6, null)]
        [InlineData("time(4)", CommonDatabaseType.Time, 4, null)]
        [InlineData("time(4) without time zone", CommonDatabaseType.Time, 4, null)]
        [InlineData("timestamp", CommonDatabaseType.DateTime, 6, null)]
        [InlineData("timestamp(3)", CommonDatabaseType.DateTime, 3, null)]
        [InlineData("timestamp without time zone", CommonDatabaseType.DateTime, 6, null)]
        [InlineData("timestamp with time zone", CommonDatabaseType.DateTimeOffset, 6, null)]
        [InlineData("timestamp(1) without time zone", CommonDatabaseType.DateTime, 1, null)]
        [InlineData("timestamp(1) with time zone", CommonDatabaseType.DateTimeOffset, 1, null)]

        public async Task ShouldMapToCommonDataType(string storeType, CommonDatabaseType commonDbType, int? arg1, int? arg2)
        {
            _dbConnection.ExecuteNonQuery($"create table table1(id {storeType});");
            var databaseDescriptor = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            var columnStoreType = databaseDescriptor.Tables.SelectMany(p => p.Columns).Select(p => p.StoreType).Single();
            var commonDataType = _databaseTypeMapper.ToCommonDatabaseType(columnStoreType);
            commonDataType.Should().BeEquivalentTo( new DatabaseTypeDescriptor{DbType = commonDbType,Arg1 = arg1,Arg2 = arg2});

        }
        [Theory]
        [ClassData(typeof(MapToTargetDataTypeTestData))]
        public async Task ShouldMapToTargetDataType(DatabaseTypeDescriptor databaseTypeDescriptor, string targetStoreType)
        {
            var targetType = _databaseTypeMapper.ToDatabaseStoreType(databaseTypeDescriptor);
            _dbConnection.ExecuteNonQuery($"create table table1(id {targetType});");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            var targetTypeInDatabase =  databaseDesc.Tables.SelectMany(p => p.Columns).Select(p => p.StoreType).First();
            targetTypeInDatabase.Should().Be(targetStoreType);
        }


        class MapToTargetDataTypeTestData:List<object[]>
        {
            public MapToTargetDataTypeTestData()
            {
              
               Add(DatabaseTypeDescriptor.Boolean(),"boolean" );
               Add(DatabaseTypeDescriptor.TinyInt(),"smallint" );
               Add(DatabaseTypeDescriptor.SmallInt(),"smallint" );
               Add(DatabaseTypeDescriptor.Int(),"integer" );
               Add(DatabaseTypeDescriptor.BigInt(),"bigint" );


               Add(DatabaseTypeDescriptor.Uuid(),"uuid" );
               Add(DatabaseTypeDescriptor.Text(),"text" );
               Add(DatabaseTypeDescriptor.NText(),"text" );
               Add(DatabaseTypeDescriptor.Blob(),"bytea" );
               Add(DatabaseTypeDescriptor.Float(),"real" );
               Add(DatabaseTypeDescriptor.Double(),"double precision" );
               Add(DatabaseTypeDescriptor.Decimal(20,4),"numeric(20,4)" );
               
               Add(DatabaseTypeDescriptor.Char(2),"character(2)" );
               Add(DatabaseTypeDescriptor.NChar(2),"character(2)" );
               Add(DatabaseTypeDescriptor.Varchar(2),"character varying(2)" );
               Add(DatabaseTypeDescriptor.NVarchar(2),"character varying(2)" );
               
               Add(DatabaseTypeDescriptor.Binary(1),"bytea" );
               Add(DatabaseTypeDescriptor.Varbinary(10),"bytea" );
               
               Add(DatabaseTypeDescriptor.Date(),"date" );
               Add(DatabaseTypeDescriptor.Time(2),"time(2) without time zone" );
               Add(DatabaseTypeDescriptor.DateTime(2),"timestamp(2) without time zone" );
               Add(DatabaseTypeDescriptor.DateTimeOffset(2),"timestamp(2) with time zone" );
            }

            private void Add(DatabaseTypeDescriptor descriptor, string targetStoreType)
            {
                this.Add(new Object []{descriptor, targetStoreType});
            }
        }
    }
}
