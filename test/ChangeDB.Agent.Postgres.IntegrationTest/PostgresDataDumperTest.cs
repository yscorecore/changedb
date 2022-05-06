﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;
using FluentAssertions;
using TestDB;
using Xunit;
using static ChangeDB.Agent.Postgres.PostgresCommand;

namespace ChangeDB.Agent.Postgres
{

    public class PostgresDataDumperTest : BaseTest, IDisposable
    {
        private readonly IDataDumper _dataDumper = PostgresDataDumper.Default;

        private readonly IMetadataMigrator _metadataMigrator = PostgresMetadataMigrator.Default;

        private readonly AgentContext _agentContext;

        private readonly DbConnection _dbConnection;

        private readonly DatabaseEnvironment _databaseEnvironment;

        private readonly IDatabase database;


        public PostgresDataDumperTest(DatabaseEnvironment databaseEnvironment)
        {
            database = CreateDatabase(false);

            _dbConnection = database.Connection;

            _agentContext = new AgentContext
            {
                Agent = new PostgresAgent(),
                Connection = database.Connection,
                ConnectionString = database.ConnectionString
            };
            this._databaseEnvironment = databaseEnvironment;
        }
        public void Dispose()
        {
            database.Dispose();
        }

        // [Theory(Skip = "skip on ci")]
        // [InlineData(false)]
        // [InlineData(true)]
        // TODO Slip ON CI
        private async Task ShouldImportDumpDataByPsql(bool optimizeInsertion)
        {

            _dbConnection.ExecuteNonQuery(
                "create schema ts;",
                "create table ts.table1(id int primary key,nm varchar(64));",
                "INSERT INTO ts.table1(id,nm) VALUES(1,'name1');",
                "INSERT INTO ts.table1(id,nm) VALUES(2,'name2');",
                "INSERT INTO ts.table1(id,nm) VALUES(3,'name3');"
             );

            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_agentContext);
            var tableDesc = databaseDesc.Tables.Single(p => p.Name == "table1");

            var tableData = _dbConnection.ExecuteReaderAsTable("select * from ts.table1");
            var tableDataDic = _dbConnection.ExecuteReaderAsDataList("select * from ts.table1");
            // clear data
            _dbConnection.ExecuteNonQuery("delete from ts.table1");


            string dumpFile = $"dump_{Path.GetRandomFileName()}.sql";
            await using (var writer = new StreamWriter(dumpFile))
            {
                var dumpContext = new DumpSetting
                {
                    OptimizeInsertion = optimizeInsertion,
                    Writer = writer

                };
                await _dataDumper.WriteTables(ToAsyncTable(tableData), tableDesc, dumpContext);
                await writer.FlushAsync();
            }

            // can use psql insert dump file
            PSql(dumpFile, _dbConnection.Database, port: 5432);

            var importedTableDataDic = _dbConnection.ExecuteReaderAsDataList("select * from ts.table1");

            importedTableDataDic.Should().BeEquivalentTo(tableDataDic);

        }


        [Fact]
        public async Task ShouldUseInsertStatementWhenOptimizeInsertionIsFalse()
        {
            _dbConnection.ExecuteNonQuery(
                 "create schema ts;",
                 "create table ts.table1(id int primary key,nm varchar(64));",
                 "INSERT INTO ts.table1(id,nm) VALUES(1,'name1');",
                 "INSERT INTO ts.table1(id,nm) VALUES(2,'name2');",
                 "INSERT INTO ts.table1(id,nm) VALUES(3,'name3');"
              );

            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_agentContext);
            var tableDesc = databaseDesc.Tables.Single(p => p.Name == "table1");

            var tableData = _dbConnection.ExecuteReaderAsTable("select * from ts.table1");
            // clear data
            _dbConnection.ExecuteNonQuery("delete from ts.table1");


            string dumpFile = $"dump_{Path.GetRandomFileName()}.sql";
            await using (var writer = new StreamWriter(dumpFile))
            {
                var dumpContext = new DumpSetting
                {
                    OptimizeInsertion = false,
                    Writer = writer

                };
                await _dataDumper.WriteTables(ToAsyncTable(tableData), tableDesc, dumpContext);
                writer.Flush();
            }
            File.ReadAllText(dumpFile).Should().Contain("INSERT INTO", Exactly.Times(3));

        }

        [Fact]
        public async Task ShouldUseCopyStatementWhenOptimizeInsertionIsTrue()
        {
            _dbConnection.ExecuteNonQuery(
                 "create schema ts;",
                 "create table ts.table1(id int primary key,nm varchar(64));",
                 "INSERT INTO ts.table1(id,nm) VALUES(1,'name1');",
                 "INSERT INTO ts.table1(id,nm) VALUES(2,'name2');",
                 "INSERT INTO ts.table1(id,nm) VALUES(3,'name3');"
              );

            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_agentContext);
            var tableDesc = databaseDesc.Tables.Single(p => p.Name == "table1");

            var tableData1 = _dbConnection.ExecuteReaderAsTable("select * from ts.table1 limit 1");
            var tableData2 = _dbConnection.ExecuteReaderAsTable("select * from ts.table1 limit 2 offset 1");
            // clear data
            _dbConnection.ExecuteNonQuery("delete from ts.table1");


            string dumpFile = $"dump_{Path.GetRandomFileName()}.sql";
            await using (var writer = new StreamWriter(dumpFile))
            {
                var dumpContext = new DumpSetting
                {
                    OptimizeInsertion = true,
                    Writer = writer

                };
                await _dataDumper.WriteTables(ToAsyncTable(tableData1, tableData2), tableDesc, dumpContext);
                writer.Flush();
            }
            File.ReadAllText(dumpFile).Should().Contain("COPY", Exactly.Once());
        }

        private IAsyncEnumerable<DataTable> ToAsyncTable(params DataTable[] tables)
        {
            return tables.ToAsync();
        }

    }
}
