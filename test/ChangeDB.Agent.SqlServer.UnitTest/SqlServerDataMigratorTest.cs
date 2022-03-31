using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlServerDataMigratorTest : IDisposable
    {
        private readonly IDataMigrator _dataMigrator = SqlServerDataMigrator.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;


        public SqlServerDataMigratorTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;
            _migrationContext = new MigrationContext
            {
                TargetConnection = _dbConnection,
                SourceConnection = _dbConnection
            };

        }
        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }
        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {

            _dbConnection.ExecuteNonQuery(
                "create schema ts",
                "create table ts.table1(id int primary key,nm varchar(64));",
                "insert into ts.table1(id,nm) VALUES(1,'name1');",
                "insert into ts.table1(id,nm) VALUES(2,'name2');",
                "insert into ts.table1(id,nm) VALUES(3,'name3');"
            );
            var rows = await _dataMigrator.CountSourceTable(new TableDescriptor
            {
                Name = "table1",
                Schema = "ts",
            }, _migrationContext);
            rows.Should().Be(3);
        }

        [Fact]
        public async Task ShouldReturnDataTableWhenReadTableData()
        {

            _dbConnection.ExecuteNonQuery(
                "create schema ts",
                "create table ts.table1(id int primary key,nm varchar(64));",
                "insert into ts.table1(id,nm) VALUES(1,'name1');",
                "insert into ts.table1(id,nm) VALUES(2,'name2');",
                "insert into ts.table1(id,nm) VALUES(3,'name3');"
            );
            var table = await _dataMigrator.ReadSourceTable(new TableDescriptor { Name = "table1", Schema = "ts", PrimaryKey = new PrimaryKeyDescriptor { Columns = new List<string> { "id" } } },
                new PageInfo() { Limit = 1, Offset = 1 }, _migrationContext);
            table.Rows.Count.Should().Be(1);
            table.Rows[0]["id"].Should().Be(2);
            table.Rows[0]["nm"].Should().Be("name2");
        }
        [Fact]
        public async Task ShouldSuccessWhenWriteTableData()
        {
            _dbConnection.ExecuteNonQuery(
                "create schema ts",
                "create table ts.table1(id int primary key,nm varchar(64));"
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
                Schema = "ts",
                Name = "table1",
                Columns = new List<ColumnDescriptor>
                {
                    new ColumnDescriptor{ Name = "id", DataType = DataTypeDescriptor.Int()},
                    new ColumnDescriptor{Name = "nm", DataType = DataTypeDescriptor.Varchar(64)}
                }
            };
            await WriteTargetTable(table, tableDescriptor, _migrationContext);
            var data = _dbConnection.ExecuteReaderAsList<int, string>("select * from ts.table1");
            data.Should().BeEquivalentTo(new List<Tuple<int, string>> { new Tuple<int, string>(4, "name4") });
        }

        [Fact]
        public async Task ShouldInsertIdentityColumn()
        {
            _dbConnection.ExecuteNonQuery(
                "create schema ts;",
                "create table ts.table1(id int identity(1,1) primary key ,nm varchar(64));"
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
                Schema = "ts",
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
            await WriteTargetTable(table, tableDescriptor, _migrationContext);
            _dbConnection.ExecuteNonQuery("insert into ts.table1(nm) values('name6')");
            var data = _dbConnection.ExecuteReaderAsList<int, string>("select * from ts.table1");
            data.Should().BeEquivalentTo(new List<Tuple<int, string>> { new(1, "name1"), new(6, "name6") });
        }

        private async Task WriteTargetTable(DataTable data, TableDescriptor tableDescriptor,
            MigrationContext migrationContext)
        {
            await _dataMigrator.BeforeWriteTargetTable(tableDescriptor, _migrationContext);
            await _dataMigrator.WriteTargetTable(data, tableDescriptor, _migrationContext);
            await _dataMigrator.AfterWriteTargetTable(tableDescriptor, _migrationContext);
        }
    }
}
