using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDataMigratorTest : BaseTest
    {

        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {
            var _dataMigrator = SqlCeDataMigrator.Default;
            await using var database = CreateDatabase(true,
                "create table table1(id int primary key,nm nvarchar(64));",
                "insert into table1(id,nm) VALUES(1,'name1');",
                "insert into table1(id,nm) VALUES(2,'name2');",
                "insert into table1(id,nm) VALUES(3,'name3');"
            );
            var rows = await _dataMigrator.CountSourceTable(new TableDescriptor
            {
                Name = "table1",
            }, new MigrationContext { SourceConnection = database.Connection });
            rows.Should().Be(3);
        }

        [Fact]
        [Obsolete]
        public async Task ShouldReturnDataTableWhenReadTableData()
        {
            var _dataMigrator = SqlCeDataMigrator.Default;
            await using var database = CreateDatabase(true,
               "create table table1(id int primary key,nm nvarchar(64));",
                "insert into table1(id,nm) VALUES(1,'name1');",
                "insert into table1(id,nm) VALUES(2,'name2');",
                "insert into table1(id,nm) VALUES(3,'name3');"
            );
            var tableDesc = new TableDescriptor
            {
                Name = "table1",
                Columns = new List<ColumnDescriptor>
                 {
                    new ColumnDescriptor{Name="id", DataType=DataTypeDescriptor.Int()},
                    new ColumnDescriptor{Name="nm", DataType=DataTypeDescriptor.Varchar(64)}
                 }
            };
            var context = new MigrationContext
            {
                SourceConnection = database.Connection,
                Setting = new MigrationSetting(),
                Source = new AgentRunTimeInfo { Agent = new SqlCeAgent() }
            };
            var allRows = await _dataMigrator.ReadSourceRows(tableDesc, context).ToSyncList();
            var allData = allRows.Select(p => new { Id = p.Field<int>("id"), Name = p.Field<string>("nm") }).ToList();
            allData.Should().BeEquivalentTo(new[]
            {
                new { Id = 1, Name = "name1" },
                new { Id = 2, Name = "name2" },
                new { Id = 3, Name = "name3" }
            });
        }
        [Fact]
        [Obsolete]
        public async Task ShouldSuccessWhenWriteTableData()
        {

            var dataMigrator = SqlCeDataMigrator.Default;
            await using var database = CreateDatabase(false,
                "create table table1(id int primary key,nm nvarchar(64));"
            );
            var context = new MigrationContext
            {
                TargetConnection = database.Connection,
                Setting = new MigrationSetting(),
                Source = new AgentRunTimeInfo { Agent = new SqlCeAgent() }
            };

            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("nm", typeof(string));
            var row = table.NewRow();
            row["id"] = 4;
            row["nm"] = "name4";
            table.Rows.Add(row);
            var tableDescriptor = new TableDescriptor
            {
                Schema = "ts",
                Name = "table1",
                Columns = new List<ColumnDescriptor>
                {
                    new ColumnDescriptor{ Name = "id", DataType = DataTypeDescriptor.Int()},
                    new ColumnDescriptor{Name = "nm", DataType = DataTypeDescriptor.Varchar(64)}
                }
            };
            await WriteTargetTable(dataMigrator, table, tableDescriptor, context);
            var data = database.Connection.ExecuteReaderAsList<int, string>("select * from table1");
            data.Should().BeEquivalentTo(new List<Tuple<int, string>> { new Tuple<int, string>(4, "name4") });
        }

        [Fact]
        [Obsolete]
        public async Task ShouldInsertIdentityColumn()
        {
            var dataMigrator = SqlCeDataMigrator.Default;
            await using var database = CreateDatabase(false,
                "create table table1(id int identity(1,1) primary key ,nm nvarchar(64));"
            );
            var context = new MigrationContext
            {
                TargetConnection = database.Connection,
                Setting = new MigrationSetting(),
                Source = new AgentRunTimeInfo { Agent = new SqlCeAgent() }
            };


            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("nm", typeof(string));
            var row = table.NewRow();
            row["id"] = 1;
            row["nm"] = "name1";
            table.Rows.Add(row);
            var tableDescriptor = new TableDescriptor
            {
                Name = "table1",
                Columns = new List<ColumnDescriptor>
                {
                    new ColumnDescriptor
                    {
                        Name = "id",  DataType = DataTypeDescriptor.Int(), IsIdentity = true,
                        IdentityInfo = new IdentityDescriptor
                        {
                            IsCyclic =false,
                            CurrentValue = 5
                        }
                    },
                    new ColumnDescriptor{Name = "nm",DataType = DataTypeDescriptor.Varchar(64)}
                }
            };
            await WriteTargetTable(dataMigrator, table, tableDescriptor, context);
            database.Connection.ExecuteNonQuery("insert into table1(nm) values('name6')");
            var data = database.Connection.ExecuteReaderAsList<int, string>("select * from table1");
            data.Should().BeEquivalentTo(new List<Tuple<int, string>> { new(1, "name1"), new(6, "name6") });
        }

        private async Task WriteTargetTable(IDataMigrator dataMigrator, DataTable data, TableDescriptor tableDescriptor,
            MigrationContext migrationContext)
        {
            await dataMigrator.BeforeWriteTargetTable(tableDescriptor, migrationContext);
            await dataMigrator.WriteTargetTable(data, tableDescriptor, migrationContext);
            await dataMigrator.AfterWriteTargetTable(tableDescriptor, migrationContext);
        }
    }
}
