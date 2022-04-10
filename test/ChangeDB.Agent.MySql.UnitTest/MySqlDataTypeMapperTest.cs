using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.MySql.UnitTest
{
    [Collection(nameof(DatabaseEnvironment))]
    public class MySqlDataTypeMapperTest : IDisposable
    {
        private readonly MySqlDataTypeMapper _dataTypeMapper = MySqlDataTypeMapper.Default;
        private readonly IMetadataMigrator _metadataMigrator = MySqlMetadataMigrator.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;

        [Obsolete]
        public MySqlDataTypeMapperTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;
            _migrationContext = new MigrationContext
            {
                TargetConnection = _dbConnection,
                SourceConnection = _dbConnection,
                Source = new AgentRunTimeInfo { Agent = new MySqlAgent() },
                SourceDatabase = new DatabaseInfo() { ConnectionString = databaseEnvironment.NewConnectionString(_dbConnection.Database) }
            };
        }
        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }

        [Theory]
        [InlineData("bit(1)", CommonDataType.Boolean, null, null)]
        [InlineData("bit", CommonDataType.Boolean, null, null)]

        [InlineData("tinyint(1)", CommonDataType.Boolean, null, null)]
        [InlineData("int", CommonDataType.Int, null, null)]
        [InlineData("tinyint", CommonDataType.TinyInt, null, null)]
        [InlineData("smallint", CommonDataType.SmallInt, null, null)]
        [InlineData("mediumint", CommonDataType.Int, null, null)]
        [InlineData("bigint", CommonDataType.BigInt, null, null)]
        [InlineData("int unsigned", CommonDataType.Int, null, null)]
        [InlineData("tinyint unsigned", CommonDataType.TinyInt, null, null)]
        [InlineData("smallint unsigned", CommonDataType.SmallInt, null, null)]
        [InlineData("mediumint unsigned", CommonDataType.Int, null, null)]
        [InlineData("bigint unsigned", CommonDataType.BigInt, null, null)]

        [InlineData("double", CommonDataType.Double, null, null)]
        [InlineData("float", CommonDataType.Float, null, null)]
        [InlineData("float(1)", CommonDataType.Float, null, null)]
        [InlineData("float(24)", CommonDataType.Float, null, null)]
        [InlineData("float(25)", CommonDataType.Double, null, null)]
        [InlineData("real", CommonDataType.Double, null, null)]


        [InlineData("decimal", CommonDataType.Decimal, 10, 0)]
        [InlineData("decimal(3)", CommonDataType.Decimal, 3, 0)]
        [InlineData("decimal(12,3)", CommonDataType.Decimal, 12, 3)]

        [InlineData("numeric", CommonDataType.Decimal, 10, 0)]
        [InlineData("numeric(3)", CommonDataType.Decimal, 3, 0)]
        [InlineData("numeric(12,3)", CommonDataType.Decimal, 12, 3)]


        [InlineData("year", CommonDataType.Int, null, null)]
        [InlineData("date", CommonDataType.Date, null, null)]
        [InlineData("time", CommonDataType.Time, 0, null)]
        [InlineData("time(4)", CommonDataType.Time, 4, null)]
        [InlineData("timestamp", CommonDataType.DateTime, 0, null)]
        [InlineData("timestamp(3)", CommonDataType.DateTime, 3, null)]
        [InlineData("datetime", CommonDataType.DateTime, 0, null)]
        [InlineData("datetime(3)", CommonDataType.DateTime, 3, null)]

        // [InlineData("enum(\"A\",\"B\",\"C\")", CommonDataType.NVarchar, 1, null)]
        // [InlineData("set(\"A\",\"B\",\"C\")", CommonDataType.NVarchar, 5, null)]
        [InlineData("varchar(12)", CommonDataType.NVarchar, 12, null)]
        [InlineData("char", CommonDataType.NChar, 1, null)]
        [InlineData("char(12)", CommonDataType.NChar, 12, null)]
        [InlineData("nvarchar(12)", CommonDataType.NVarchar, 12, null)]
        [InlineData("nchar(12)", CommonDataType.NChar, 12, null)]
        [InlineData("text", CommonDataType.NText, null, null)]
        [InlineData("tinytext", CommonDataType.NText, null, null)]
        [InlineData("mediumtext", CommonDataType.NText, null, null)]
        [InlineData("longtext", CommonDataType.NText, null, null)]
        [InlineData("json", CommonDataType.NText, null, null)]

        [InlineData("binary", CommonDataType.Binary, 1, null)]
        [InlineData("binary(12)", CommonDataType.Binary, 12, null)]
        [InlineData("binary(16)", CommonDataType.Uuid, null, null)]
        [InlineData("varbinary(12)", CommonDataType.Varbinary, 12, null)]
        [InlineData("varbinary(16)", CommonDataType.Varbinary, 16, null)]
        [InlineData("blob", CommonDataType.Blob, null, null)]
        [InlineData("tinyblob", CommonDataType.Blob, null, null)]
        [InlineData("mediumblob", CommonDataType.Blob, null, null)]
        [InlineData("longblob", CommonDataType.Blob, null, null)]
        [Obsolete]
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

                Add(DataTypeDescriptor.Boolean(), "tinyint(1)");
                Add(DataTypeDescriptor.TinyInt(), "tinyint");
                Add(DataTypeDescriptor.SmallInt(), "smallint");
                Add(DataTypeDescriptor.Int(), "int");
                Add(DataTypeDescriptor.BigInt(), "bigint");


                Add(DataTypeDescriptor.Uuid(), "binary(16)");
                Add(DataTypeDescriptor.Text(), "longtext");
                Add(DataTypeDescriptor.NText(), "longtext");
                Add(DataTypeDescriptor.Blob(), "longblob");
                Add(DataTypeDescriptor.Float(), "float");
                Add(DataTypeDescriptor.Double(), "double");
                Add(DataTypeDescriptor.Decimal(20, 4), "decimal(20,4)");

                Add(DataTypeDescriptor.Char(2), "char(2)");
                Add(DataTypeDescriptor.NChar(2), "char(2)");
                Add(DataTypeDescriptor.Varchar(2), "varchar(2)");
                Add(DataTypeDescriptor.NVarchar(2), "varchar(2)");

                Add(DataTypeDescriptor.Binary(1), "binary(1)");
                Add(DataTypeDescriptor.Varbinary(10), "varbinary(10)");

                Add(DataTypeDescriptor.Date(), "date");
                Add(DataTypeDescriptor.Time(2), "time(2)");
                Add(DataTypeDescriptor.DateTime(2), "datetime(2)");
                Add(DataTypeDescriptor.DateTimeOffset(2), "datetime(2)");
            }

            private void Add(DataTypeDescriptor descriptor, string targetStoreType)
            {
                this.Add(new Object[] { descriptor, targetStoreType });
            }
        }
    }
}
