using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresMetadataMigratorTest

    {
        private const string NpgConn = "Server=127.0.0.1;Port=5432;Database=testdb;User Id=postgres;Password=mypassword;";
        private readonly PostgresMetadataMigrator _metadataMigrator = PostgresMetadataMigrator.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting();
        private readonly DatabaseInfo _databaseInfo = new DatabaseInfo()
        {
            Type = "POSTGRES",
            Connection = new NpgsqlConnection(NpgConn)
        };

        public PostgresMetadataMigratorTest()
        {
            using var conn = new NpgsqlConnection(NpgConn);
            conn.ReCreateDatabase();
            conn.ExecuteNonQuery(
                "create schema ts",
                "create table ts.table1(id int primary key,nm varchar(64));"
            );
        }
        [Fact]
        public async Task ShouldSuccessWhenGetTableDescription()
        {
            var tableDesc = await _metadataMigrator.GetDatabaseDescriptor(_databaseInfo, _migrationSetting);
            tableDesc.Should().NotBeNull();
            tableDesc.Should().BeEquivalentTo(new DatabaseDescriptor
            {
                Name = "testdb",
                Schemas = new List<string> { "ts" },
                Tables = new List<TableDescriptor>
                   {
                        new TableDescriptor
                        {
                            Name="table1",
                            Schema="ts",
                            Columns = new List<ColumnDescriptor>
                            {
                              new ColumnDescriptor{ Name ="id",IsPrimaryKey =true,AllowNull = false},
                              new ColumnDescriptor{ Name ="nm",IsPrimaryKey =false,AllowNull =true}
                            }
                        }
                   }
            });

        }
    }
}
