﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlCeDataMigratorTest : IDisposable
    {
        private readonly IDataMigrator _dataMigrator = new SqlCeMigrationAgent().DataMigrator;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;


        public SqlCeDataMigratorTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;

            _migrationContext = new MigrationContext
            {
                TargetConnection = _dbConnection,
                SourceConnection = _dbConnection
            };

            _dbConnection.ExecuteNonQuery(

               "create table table1(id int primary key,nm nvarchar(64));",
               "insert into table1(id,nm) VALUES(1,'name1');",
               "insert into table1(id,nm) VALUES(2,'name2');",
               "insert into table1(id,nm) VALUES(3,'name3');"
           );
        }
        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }
        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {
            var rows = await _dataMigrator.CountSourceTable(new TableDescriptor
            {
                Name = "table1",
                Schema = null,
            }, _migrationContext);
            rows.Should().Be(3);
        }

        [Fact]
        public async Task ShouldReturnDataTableWhenReadTableData()
        {
            var table = await _dataMigrator.ReadSourceTable(new TableDescriptor { Name = "table1", Schema = null, PrimaryKey = new PrimaryKeyDescriptor { Columns = new List<string> { "id" } } },
                new PageInfo() { Limit = 1, Offset = 1 }, _migrationContext);
            table.Rows.Count.Should().Be(1);
            table.Rows[0]["id"].Should().Be(2);
            table.Rows[0]["nm"].Should().Be("name2");
        }
        [Fact]
        public async Task ShouldSuccessWhenWriteTableData()
        {


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
                    new ColumnDescriptor{ Name = "id"},
                    new ColumnDescriptor{Name = "nm"}
                }
            };
            await _dataMigrator.WriteTargetTable(table, tableDescriptor, _migrationContext);
            var totalRows = await _dataMigrator.CountSourceTable(tableDescriptor, _migrationContext);
            totalRows.Should().Be(4);
        }

    }
}
