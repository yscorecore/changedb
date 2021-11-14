using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe.UnitTest
{
   public  class SqlCeDataMigratorTest
    {
        private readonly SqlCeDataMigrator _dataMigrator = SqlCeDataMigrator.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting();
        private readonly DbConnection _dbConnection;


        public SqlCeDataMigratorTest()
        {
            _dbConnection = new SqlCeConnection($"Data Source={System.IO.Path.GetTempFileName()}.sdf;Persist Security Info=False;");

            _dbConnection.CreateDatabase();
        }

        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {
            _dbConnection.ExecuteNonQuery(
                "create schema ts",
                "create table ts.table1(id int primary key,nm varchar(64));",
                "insert into ts.table1(id,nm) values(1,'name1');",
                "insert into ts.table1(id,nm) values(2,'name2');",
                "insert into ts.table1(id,nm) values(3,'name3');"
            );

            var rows = await _dataMigrator.CountTable(new TableDescriptor
            {
                Name = "table1",
                Schema = "ts",
            }, _dbConnection, _migrationSetting);
            rows.Should().Be(3);
        }

    }
}
