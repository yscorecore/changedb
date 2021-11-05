using System.Data;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresDataMigratorTest
    {
        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {
            var dataMigrator = PostgresDataMigrator.Default;
            var rows = await  dataMigrator.CountTable(new TableDescriptor
            {
                 Name = "table1",
                 Schema = "ts",
            }, new MigrationSetting());
           rows.Should().Be(3);
        }
        
    }
}
