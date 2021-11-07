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
                              new ColumnDescriptor{ Name ="nm",IsPrimaryKey =false,AllowNull =true,DbType  = new DBTypeDescriptor{ DbType= DBType.Character__Varying, Length=64 }}
                            }
                        }
                   }
            };
            await _metadataMigrator.PreMigrate(databaseDesc, _dbConnection, _migrationSetting);
            var schemas = _dbConnection.ExecuteReaderAsList<string>("select table_schema || '.' ||table_name from information_schema.\"tables\" t where t.table_schema not in ('pg_catalog','pg_toast','information_schema')");
            schemas.Should().Contain("ts.table1");
        }
        public void Dispose()
        {
            _dbConnection?.Dispose();
        }
    }
}
