using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataMigratorTest
    {
        private const string NpgConn = "Server=127.0.0.1;Port=5432;Database=testdb;User Id=postgres;Password=mypassword;";
        public PostgresDataMigratorTest()
        {
            using (var conn = new NpgsqlConnection(NpgConn))
            {
                conn.ReCreateDatabase();
                conn.ExecuteNonQuery(
                    "create schema ts",
                    "create table ts.table1(id int primary key,nm varchar(64));",
                    "insert into ts.table1(id,nm) values(1,'name1');",
                    "insert into ts.table1(id,nm) values(2,'name1');",
                    "insert into ts.table1(id,nm) values(3,'name1');"
                );
            }
        }

        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {
            var dataMigrator = PostgresDataMigrator.Default;
            var migrationSetting = new MigrationSetting();

            var databaseInfo = new DatabaseInfo()
            {
                Type = "POSTGRES",
                Connection = new NpgsqlConnection(NpgConn)
            };
            var rows = await dataMigrator.CountTable(new TableDescriptor
            {
                Name = "table1",
                Schema = "ts",
            }, databaseInfo, migrationSetting);
            rows.Should().Be(3);
        }

    }
}
