using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataMigratorTest : System.IDisposable
    {
        private const string NpgConn = "Server=127.0.0.1;Port=5432;Database=testdb;User Id=postgres;Password=mypassword;";
        private readonly PostgresDataMigrator _dataMigrator = PostgresDataMigrator.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting();
        private readonly DatabaseInfo _databaseInfo = new DatabaseInfo()
        {
            Type = "POSTGRES",
            Connection = new NpgsqlConnection(NpgConn)
        };
        public PostgresDataMigratorTest()
        {
            using var conn = new NpgsqlConnection(NpgConn);
            conn.ReCreateDatabase();
            conn.ExecuteNonQuery(
                "create schema ts",
                "create table ts.table1(id int primary key,nm varchar(64));",
                "insert into ts.table1(id,nm) values(1,'name1');",
                "insert into ts.table1(id,nm) values(2,'name2');",
                "insert into ts.table1(id,nm) values(3,'name3');"
            );
        }

        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {
            var rows = await _dataMigrator.CountTable(new TableDescriptor
            {
                Name = "table1",
                Schema = "ts",
            }, _databaseInfo, _migrationSetting);
            rows.Should().Be(3);
        }

        [Fact]
        public async Task ShouldReturnDataTableWhenReadTableData()
        {

            var table = await _dataMigrator.ReadTableData(new TableDescriptor { Name = "table1", Schema = "ts", },
                new PageInfo() { Limit = 1, Offset = 1 }, _databaseInfo, _migrationSetting);
            table.Rows.Count.Should().Be(1);
            table.Rows[0]["id"].Should().Be(2);
            table.Rows[0]["nm"].Should().Be("name2");
        }

        public void Dispose()
        {
            _databaseInfo.Dispose();
        }
    }
}
