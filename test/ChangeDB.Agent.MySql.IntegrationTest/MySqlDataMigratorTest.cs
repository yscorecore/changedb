using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using TestDB;
using Xunit;

namespace ChangeDB.Agent.MySql
{
    public class MySqlDataMigratorTest : BaseTest, IDisposable
    {

        private readonly IDataMigrator _dataMigrator = MySqlDataMigrator.Default;
        private readonly AgentContext _agentContext;
        private readonly DbConnection _dbConnection;
        private readonly IDatabase _database;


        public MySqlDataMigratorTest()
        {
            _database = CreateDatabase(false);
            _dbConnection = _database.Connection;

            _agentContext = new AgentContext
            {
                Connection = _dbConnection,
            };

        }
        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));",
                "INSERT INTO table1(id,nm) VALUES(1,'name1');",
                "INSERT INTO table1(id,nm) VALUES(2,'name2');",
                "INSERT INTO table1(id,nm) VALUES(3,'name3');"
            );

            var rows = await _dataMigrator.CountSourceTable(new TableDescriptor
            {
                Name = "table1",
                Schema = null,
            }, _agentContext);
            rows.Should().Be(3);
        }

        [Fact]
        public async Task ShouldReturnDataTableWhenReadTableData()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));",
                "INSERT INTO table1(id,nm) VALUES(1,'name1');",
                "INSERT INTO table1(id,nm) VALUES(2,'name2');",
                "INSERT INTO table1(id,nm) VALUES(3,'name3');"
            );
            var table = await _dataMigrator.ReadSourceTable(new TableDescriptor { Name = "table1", Schema = null, },
                new PageInfo { Limit = 1, Offset = 1 }, _agentContext);
            table.Rows.Count.Should().Be(1);
            table.Rows[0]["id"].Should().Be(2);
            table.Rows[0]["nm"].Should().Be("name2");
        }
        [Fact]
        public async Task ShouldReturnDataTableWhenReadTableDataAndNoPrimaryKey()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm varchar(64));",
                "INSERT INTO table1(id,nm) VALUES(1,'name1');",
                "INSERT INTO table1(id,nm) VALUES(2,'name2');",
                "INSERT INTO table1(id,nm) VALUES(3,'name3');"
            );
            var table = await _dataMigrator.ReadSourceTable(new TableDescriptor { Name = "table1", Schema = null, },
                new PageInfo { Limit = 1, Offset = 1 }, _agentContext);
            table.Rows.Count.Should().Be(1);
            table.Rows[0]["id"].Should().Be(2);
            table.Rows[0]["nm"].Should().Be("name2");
        }

        [Fact]
        public async Task ShouldSuccessWhenWriteTableData()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));"
            );
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("nm", typeof(string));
            var row = table.NewRow();
            row["id"] = 4;
            row["nm"] = "name4";
            table.Rows.Add(row);
            var tableDescriptor = new TableDescriptor
            {
                Schema = null,
                Name = "table1",
                Columns = new List<ColumnDescriptor>
                {
                    new ColumnDescriptor{ Name = "id",DataType = DataTypeDescriptor.Int()},
                    new ColumnDescriptor{Name = "nm",DataType = DataTypeDescriptor.Varchar(64)}
                }
            };
            await WriteTargetTable(table, tableDescriptor, _agentContext);
            var data = _dbConnection.ExecuteReaderAsList<int, string>("select * from table1");
            data.Should().BeEquivalentTo(new List<Tuple<int, string>> { new Tuple<int, string>(4, "name4") });
        }

        [Fact]
        public async Task ShouldInsertIdentityColumn()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int  primary key auto_increment ,nm varchar(64));"
            );
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("nm", typeof(string));
            var row = table.NewRow();
            row["id"] = 1;
            row["nm"] = "name1";
            table.Rows.Add(row);
            var tableDescriptor = new TableDescriptor
            {
                Schema = null,
                Name = "table1",
                Columns = new List<ColumnDescriptor>
                {
                    new ColumnDescriptor
                    {
                        Name = "id", DataType = DataTypeDescriptor.Int(), IsIdentity = true,
                        IdentityInfo = new IdentityDescriptor
                        {
                            IsCyclic =false,
                            CurrentValue = 5
                        }
                    },
                    new ColumnDescriptor{Name = "nm",DataType = DataTypeDescriptor.Varchar(64)}
                }
            };
            await WriteTargetTable(table, tableDescriptor, _agentContext);
            _dbConnection.ExecuteNonQuery("insert into table1(nm) values('name6')");
            _dbConnection.ExecuteNonQuery("insert into table1(nm) values('name7')");
            var data = _dbConnection.ExecuteReaderAsList<int, string>("select * from table1");
            data.Should().BeEquivalentTo(new List<Tuple<int, string>> { new(1, "name1"), new(6, "name6"), new(7, "name7") });
        }


        private async Task WriteTargetTable(DataTable data, TableDescriptor tableDescriptor,
            AgentContext migrationContext)
        {
            await _dataMigrator.BeforeWriteTable(tableDescriptor, migrationContext);
            await _dataMigrator.WriteTargetTable(data, tableDescriptor, migrationContext);
            await _dataMigrator.AfterWriteTable(tableDescriptor, migrationContext);
        }

    }
}
