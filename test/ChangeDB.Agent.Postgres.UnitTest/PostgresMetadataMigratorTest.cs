using System;
using System.Collections.Generic;
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
        private readonly DatabaseInfo _databaseInfo;
        private readonly string _databaseName;
        private readonly ITestOutputHelper testOutputHelper;

        public PostgresMetadataMigratorTest(ITestOutputHelper testOutputHelper)
        {
            _databaseName = TestUtils.RandomDatabaseName();
            _databaseInfo = new DatabaseInfo
            {
                Type = "POSTGRES",
                Connection = new NpgsqlConnection($"Server=127.0.0.1;Port=5432;Database={_databaseName};User Id=postgres;Password=mypassword;")
            };
            this.testOutputHelper = testOutputHelper;
        }
        [Fact]
        public async Task ShouldSuccessWhenGetTableDescription()
        {
            _databaseInfo.Connection.ReCreateDatabase();
            _databaseInfo.Connection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id int primary key,nm varchar(64));");

            var tableDesc = await _metadataMigrator.GetDatabaseDescriptor(_databaseInfo, _migrationSetting);
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
                              new ColumnDescriptor{ Name ="nm",IsPrimaryKey =false,AllowNull =true,DbType  = new DBTypeDescriptor{ DbType= DBType.Character__Varying, Length=64 }}
                            }
                        }
                   }
            });
        }

        [Fact]
        public async Task ShouldCreateSchemasWhenPreMigrate()
        {
            var databaseDesc = new DatabaseDescriptor()
            { Schemas = new List<string> { "public", "abc","Bcd" } };
            await _metadataMigrator.PreMigrate(databaseDesc, _databaseInfo, _migrationSetting);
            var schemas = _databaseInfo.Connection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata s ");
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
                              new ColumnDescriptor{ Name ="nm",IsPrimaryKey =false,AllowNull =true,DbType  = new DBTypeDescriptor{ DbType= DBType.Character__Varying, Length=64 }}
                            }
                        }
                   }
            };
            await _metadataMigrator.PreMigrate(databaseDesc, _databaseInfo, _migrationSetting);
            var schemas = _databaseInfo.Connection.ExecuteReaderAsList<string>("select table_schema || '.' ||table_name from information_schema.\"tables\" t where t.table_schema not in ('pg_catalog','pg_toast','information_schema')");
            schemas.Should().Contain("ts.table1");
        }
        public void Dispose()
        {
            _databaseInfo?.Dispose();
        }
    }
}
