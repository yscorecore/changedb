﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlServerDataMigratorTest : IDisposable
    {
        private readonly IDataMigrator _dataMigrator = SqlServerDataMigrator.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting();
        private readonly DbConnection _dbConnection;


        public SqlServerDataMigratorTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;
            _dbConnection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id int primary key,nm varchar(64));",
               "insert into ts.table1(id,nm) values(1,'name1');",
               "insert into ts.table1(id,nm) values(2,'name2');",
               "insert into ts.table1(id,nm) values(3,'name3');"
           );
        }
        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }
        [Fact]
        public async Task ShouldReturnTableRowCountWhenCountTable()
        {


            var rows = await _dataMigrator.CountTable(new TableDescriptor
            {
                Name = "table1",
                Schema = "ts",
            }, _dbConnection, _migrationSetting);
            rows.Should().Be(3);
        }

        [Fact]
        public async Task ShouldReturnDataTableWhenReadTableData()
        {


            var table = await _dataMigrator.ReadTableData(new TableDescriptor { Name = "table1", Schema = "ts", PrimaryKey = new PrimaryKeyDescriptor { Columns = new List<string> { "id" } } },
                new PageInfo() { Limit = 1, Offset = 1 }, _dbConnection, _migrationSetting);
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
                Schema = "ts",
                Name = "table1",
                Columns = new List<ColumnDescriptor>
                {
                    new ColumnDescriptor{ Name = "id"},
                    new ColumnDescriptor{Name = "nm"}
                }
            };
            await _dataMigrator.WriteTableData(table, tableDescriptor, _dbConnection, _migrationSetting);
            var totalRows = await _dataMigrator.CountTable(tableDescriptor, _dbConnection, _migrationSetting);
            totalRows.Should().Be(4);
        }

    }
}