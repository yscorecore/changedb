using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresMetadataMigratorTest : IDisposable

    {
        private readonly PostgresMetadataMigrator _metadataMigrator = PostgresMetadataMigrator.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting { DropTargetDatabaseIfExists = true };
        private readonly DbConnection _dbConnection;

        public PostgresMetadataMigratorTest()
        {
            _dbConnection = new NpgsqlConnection($"Server=127.0.0.1;Port=5432;Database={TestUtils.RandomDatabaseName()};User Id=postgres;Password=mypassword;");
        }
        [Fact]
        public async Task ShouldSuccessWhenGetTableDescription()
        {
            _dbConnection.ReCreateDatabase();
            _dbConnection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id int primary key,nm varchar(64));");

            var tableDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            tableDesc.Should().NotBeNull();
            tableDesc.Should().BeEquivalentTo(new DatabaseDescriptor
            {
                Schemas = new List<string> { "ts" },
                Tables = new List<TableDescriptor>
                   {
                        new TableDescriptor
                        {
                            Name="table1",
                            Schema="ts",
                            Columns = new List<ColumnDescriptor>
                            {
                              new ColumnDescriptor{ Name ="id",IsPrimaryKey =true,AllowNull = false,DbType  = new DBTypeDescriptor{ DbType= DBType.Int } },
                              new ColumnDescriptor{ Name ="nm",IsPrimaryKey =false,AllowNull =true,DbType  = new DBTypeDescriptor{ DbType= DBType.NVarchar, Length=64 }}
                            }
                        }
                   }
            });
        }

        [Fact]
        public async Task ShouldCreateSchemasWhenPreMigrate()
        {
            var databaseDesc = new DatabaseDescriptor()
            { Schemas = new List<string> { "public", "abc", "Bcd" } };
            await _metadataMigrator.PreMigrate(databaseDesc, _dbConnection, _migrationSetting);
            var schemas = _dbConnection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata s ");
            schemas.Should().Contain(databaseDesc.Schemas);
        }
        [Fact]
        public async Task ShouldCreateTableWhenPreMigrate()
        {
            var databaseDesc = new DatabaseDescriptor
            {
                Schemas = new List<string> { "ts" },
                Tables = new List<TableDescriptor>
                   {
                        new TableDescriptor
                        {
                            Name="table1",
                            Schema="ts",
                            Columns = new List<ColumnDescriptor>
                            {
                              new ColumnDescriptor{ Name ="id",IsPrimaryKey =true,AllowNull = false,DbType  = new DBTypeDescriptor{ DbType= DBType.Int } },
                              new ColumnDescriptor{ Name ="nm",IsPrimaryKey =false,AllowNull =true,DbType  = new DBTypeDescriptor{ DbType= DBType.NVarchar, Length=64 }}
                            }
                        }
                   }
            };
            await _metadataMigrator.PreMigrate(databaseDesc, _dbConnection, _migrationSetting);
            var databaseDesc2 = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            databaseDesc2.Should().BeEquivalentTo(databaseDesc);
        }
        [Theory]

        [InlineData("varchar(12)", DBType.NVarchar, 12, null)]
        [InlineData("character varying(12)", DBType.NVarchar, 12, null)]
        [InlineData("char(12)", DBType.NChar, 12, null)]
        [InlineData("character(12)", DBType.NChar, 12, null)]
        [InlineData("text", DBType.NText, null, null)]
        [InlineData("varchar", DBType.NText, null, null)]
        [InlineData("char", DBType.NChar, 1, null)]
        [InlineData("int", DBType.Int, null, null)]
        [InlineData("integer", DBType.Int, null, null)]
        [InlineData("smallint", DBType.SmallInt, null, null)]
        [InlineData("bigint", DBType.BigInt, null, null)]

       // [InlineData("serial", DBType.Int, null, null)]
       // [InlineData("bigserial", DBType.BigInt, null, null)]

        [InlineData("decimal", DBType.Decimal, null, null)]
        [InlineData("decimal(3)", DBType.Decimal, 3, 0)]
        [InlineData("decimal(12,3)", DBType.Decimal, 12, 3)]
        [InlineData("dec", DBType.Decimal, null, null)]
        [InlineData("dec(3)", DBType.Decimal, 3, 0)]
        [InlineData("dec(12,3)", DBType.Decimal, 12, 3)]

        [InlineData("numeric", DBType.Decimal, null, null)]
        [InlineData("numeric(3)", DBType.Decimal, 3, 0)]
        [InlineData("numeric(12,3)", DBType.Decimal, 12, 3)]
        [InlineData("money", DBType.Decimal, 19, 2)]
        [InlineData("double precision", DBType.Double, null, null)]
        [InlineData("float", DBType.Double, null, null)]
        [InlineData("float(1)", DBType.Float, null, null)]
        [InlineData("float(24)", DBType.Float, null, null)]
        [InlineData("float(25)", DBType.Double, null, null)]
        [InlineData("real", DBType.Float, null, null)]
        [InlineData("uuid", DBType.Uuid, null, null)]
        [InlineData("date", DBType.Date, null, null)]
        [InlineData("time", DBType.Time, 6, null)]
        [InlineData("time(4)", DBType.Time, 4, null)]
        [InlineData("time(4) without time zone", DBType.Time, 4, null)]
        [InlineData("timestamp", DBType.DateTime, 6, null)]
        [InlineData("timestamp(3)", DBType.DateTime, 3, null)]
        [InlineData("timestamp without time zone", DBType.DateTime, 6, null)]
        [InlineData("timestamp with time zone", DBType.DateTimeOffset, 6, null)]
        [InlineData("timestamp(1) without time zone", DBType.DateTime, 1, null)]
        [InlineData("timestamp(1) with time zone", DBType.DateTimeOffset, 1, null)]

        public async Task ShouldMapDataTypeTo(string postgresDataType, DBType commonDbType, int? length, int? accuracy)
        {
            _dbConnection.ReCreateDatabase();
            _dbConnection.ExecuteNonQuery($"create table table1(col1 {postgresDataType})");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            databaseDesc.Should().BeEquivalentTo(new DatabaseDescriptor
            {
                Schemas = new List<string> { "public" },
                Tables = new List<TableDescriptor>
                 {
                     new TableDescriptor
                     {
                         Name = "table1",
                         Schema = "public",
                         Columns = new List<ColumnDescriptor>
                         {
                             new ColumnDescriptor{ Name = "col1",AllowNull = true, DbType = new DBTypeDescriptor{ DbType = commonDbType,Length = length, Accuracy = accuracy} }
                         }
                     }
                 }
            });
        }

        public void Dispose()
        {
            _dbConnection?.Dispose();
        }
    }
}
