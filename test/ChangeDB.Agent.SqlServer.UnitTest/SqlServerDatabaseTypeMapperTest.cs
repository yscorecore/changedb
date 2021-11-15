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
    public class SqlServerDatabaseTypeMapperTest
    {
        private readonly IMetadataMigrator _metadataMigrator = SqlServerMetadataMigrator.Default;
        private readonly IDatabaseTypeMapper _databaseTypeMapper=SqlServerDatabaseTypeMapper.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting { DropTargetDatabaseIfExists = true };
        private readonly DbConnection _dbConnection;
        private readonly string _connectionString;

        public SqlServerDatabaseTypeMapperTest()
        {
            _connectionString = $"Server=127.0.0.1,1433;Database={TestUtils.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;";
            _dbConnection = new SqlConnection(_connectionString);
            _dbConnection.CreateDatabase();
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
        [InlineData("time",CommonDatabaseType.Time,null,null)]
        [InlineData("datetime",CommonDatabaseType.DateTime,null,null)]
        [InlineData("datetime2",CommonDatabaseType.DateTime,null,null)]
        [InlineData("datetimeoffset",CommonDatabaseType.DateTimeOffset,null,null)]
       
        [InlineData("time(1)",CommonDatabaseType.Time,1,null)]
        [InlineData("datetime2(1)",CommonDatabaseType.DateTime,1,null)]
        [InlineData("datetimeoffset(1)",CommonDatabaseType.DateTimeOffset,1,null)]
        public async Task ShouldMapToCommonDataType(string storeType, CommonDatabaseType commonDbType, int? size, int? scale)
        {
            _dbConnection.ExecuteNonQuery($"create table table1(id {storeType});");
            var databaseDescriptor = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            var columnStoreType = databaseDescriptor.Tables.SelectMany(p => p.Columns).Select(p => p.StoreType).Single();
            var commonDataType = _databaseTypeMapper.ToCommonDatabaseType(columnStoreType);
            commonDataType.Should().BeEquivalentTo(DatabaseTypeDescriptor.Create(commonDbType, size, scale));
        }
        
        // public async Task ShouldMapToTargetDataType(CommonDatabaseType commonDbType, int? size, int? scale,string storeType)
        // {
        //     _dbConnection.ExecuteNonQuery($"create table table1(id {storeType});");
        //     var databaseDescriptor = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
        //     var columnStoreType = databaseDescriptor.Tables.SelectMany(p => p.Columns).Select(p => p.StoreType).Single();
        //     var commonDataType = _databaseTypeMapper.ToCommonDatabaseType(columnStoreType);
        //     commonDataType.Should().BeEquivalentTo(DatabaseTypeDescriptor.Create(commonDbType, size, scale));
        // }
    }
}
