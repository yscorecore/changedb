using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlServerDatabaseTypeMapperTest:IDisposable
    {
        private readonly IMetadataMigrator _metadataMigrator = SqlServerMetadataMigrator.Default;
        private readonly IDatabaseTypeMapper _databaseTypeMapper=SqlServerDatabaseTypeMapper.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting { DropTargetDatabaseIfExists = true };
        private readonly DbConnection _dbConnection;

        public SqlServerDatabaseTypeMapperTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;
        }
        public void Dispose()
        {
            _dbConnection.ClearDatabase();

        }

        [Theory]
        [InlineData("bit",CommonDatabaseType.Boolean,null,null)]
        [InlineData("tinyint",CommonDatabaseType.TinyInt,null,null)]
        [InlineData("smallint",CommonDatabaseType.SmallInt,null,null)]
        [InlineData("int",CommonDatabaseType.Int,null,null)]
        [InlineData("bigint",CommonDatabaseType.BigInt,null,null)]
        [InlineData("decimal",CommonDatabaseType.Decimal,18,0)]
        [InlineData("decimal(20)",CommonDatabaseType.Decimal,20,0)]
        [InlineData("decimal(20,4)",CommonDatabaseType.Decimal,20,4)]
        [InlineData("numeric",CommonDatabaseType.Decimal,18,0)]
        [InlineData("numeric(20)",CommonDatabaseType.Decimal,20,0)]
        [InlineData("numeric(20,4)",CommonDatabaseType.Decimal,20,4)]
        [InlineData("smallmoney",CommonDatabaseType.Decimal,10,4)]
        [InlineData("money",CommonDatabaseType.Decimal,19,4)]
        [InlineData("real",CommonDatabaseType.Float,null,null)]
        [InlineData("float",CommonDatabaseType.Double,null,null)]
        [InlineData("float(24)",CommonDatabaseType.Float,null,null)]
        [InlineData("float(25)",CommonDatabaseType.Double,null,null)]
        [InlineData("float(53)",CommonDatabaseType.Double,null,null)]
        [InlineData("char(10)",CommonDatabaseType.Char,10,null)]
        [InlineData("char",CommonDatabaseType.Char,1,null)]
        [InlineData("varchar",CommonDatabaseType.Varchar,1,null)]
        [InlineData("varchar(8000)",CommonDatabaseType.Varchar,8000,null)]
        [InlineData("varchar(MAX)",CommonDatabaseType.Text,null,null)]
        [InlineData("nvarchar(4000)",CommonDatabaseType.NVarchar,4000,null)]
        [InlineData("nvarchar(MAX)",CommonDatabaseType.NText,null,null)]
        [InlineData("text",CommonDatabaseType.Text,null,null)]
        [InlineData("ntext",CommonDatabaseType.NText,null,null)]
        [InlineData("xml",CommonDatabaseType.NText,null,null)]
        
        [InlineData("binary",CommonDatabaseType.Binary,1,null)]
        [InlineData("binary(10)",CommonDatabaseType.Binary,10,null)]
        [InlineData("varbinary",CommonDatabaseType.Varbinary,1,null)]
        [InlineData("varbinary(8000)",CommonDatabaseType.Varbinary,8000,null)]
        [InlineData("varbinary(MAX)",CommonDatabaseType.Blob,null,null)]
        [InlineData("timestamp",CommonDatabaseType.Binary,8,null)]
        [InlineData("rowversion",CommonDatabaseType.Binary,8,null)]
        [InlineData("image",CommonDatabaseType.Blob,null,null)]
        
        [InlineData("uniqueidentifier",CommonDatabaseType.Uuid,null,null)]
        
        [InlineData("date",CommonDatabaseType.Date,null,null)]
        [InlineData("time",CommonDatabaseType.Time,0,null)]
        [InlineData("datetime",CommonDatabaseType.DateTime,0,null)]
        [InlineData("datetime2",CommonDatabaseType.DateTime,0,null)]
        [InlineData("datetimeoffset",CommonDatabaseType.DateTimeOffset,0,null)]
       
        [InlineData("time(1)",CommonDatabaseType.Time,1,null)]
        [InlineData("datetime2(1)",CommonDatabaseType.DateTime,1,null)]
        [InlineData("datetimeoffset(1)",CommonDatabaseType.DateTimeOffset,1,null)]
        public async Task ShouldMapToCommonDataType(string storeType, CommonDatabaseType commonDbType, int? arg1, int? arg2)
        {
            _dbConnection.ExecuteNonQuery($"create table table1(id {storeType});");
            var databaseDescriptor = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            var columnStoreType = databaseDescriptor.Tables.SelectMany(p => p.Columns).Select(p => p.StoreType).Single();
            var commonDataType = _databaseTypeMapper.ToCommonDatabaseType(columnStoreType);
            commonDataType.Should().BeEquivalentTo( new DatabaseTypeDescriptor{DbType = commonDbType,Arg1 = arg1,Arg2 = arg2});
        }
        [Theory]
        [InlineData("bit",CommonDatabaseType.Boolean,null,null)]
        [InlineData("tinyint",CommonDatabaseType.TinyInt,null,null)]
        [InlineData("smallint",CommonDatabaseType.SmallInt,null,null)]
        [InlineData("int",CommonDatabaseType.Int,null,null)]
        [InlineData("bigint",CommonDatabaseType.BigInt,null,null)]
        [InlineData("decimal(20, 4)",CommonDatabaseType.Decimal,20,4)]
        [InlineData("real",CommonDatabaseType.Float,null,null)]
        [InlineData("float",CommonDatabaseType.Double,null,null)]
        
        [InlineData("char(10)",CommonDatabaseType.Char,10,null)]

        [InlineData("varchar(8000)",CommonDatabaseType.Varchar,8000,null)]
        [InlineData("nvarchar(4000)",CommonDatabaseType.NVarchar,4000,null)]
        [InlineData("text",CommonDatabaseType.Text,null,null)]
        [InlineData("ntext",CommonDatabaseType.NText,null,null)]
        
        [InlineData("binary(10)",CommonDatabaseType.Binary,10,null)]
        [InlineData("varbinary(8000)",CommonDatabaseType.Varbinary,8000,null)]
        [InlineData("image",CommonDatabaseType.Blob,null,null)]
        
        [InlineData("uniqueidentifier",CommonDatabaseType.Uuid,null,null)]
        
        [InlineData("date",CommonDatabaseType.Date,null,null)]
        [InlineData("time(2)",CommonDatabaseType.Time,2,null)]
        [InlineData("datetime2(2)",CommonDatabaseType.DateTime,2,null)]
        [InlineData("datetimeoffset(3)",CommonDatabaseType.DateTimeOffset,3,null)]

        public async Task ShouldMapToTargetDataType(string targetStoreType, CommonDatabaseType commonDbType, int? size, int? scale)
        {
            var databaseTypeDescriptor = new DatabaseTypeDescriptor {DbType = commonDbType, Arg1 = size, Arg2 = scale};
            var targetType = _databaseTypeMapper.ToDatabaseStoreType(databaseTypeDescriptor);
            _dbConnection.ExecuteNonQuery($"create table table1(id {targetType});");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            var targetTypeInDatabase =  databaseDesc.Tables.SelectMany(p => p.Columns).Select(p => p.StoreType).First();
            targetTypeInDatabase.Should().Be(targetStoreType);
        }
    }
}
