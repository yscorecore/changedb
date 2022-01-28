using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChangeDB.Agent.Postgres
{

    public class PostgresDataDumperTest
    {
        private readonly IDataDumper _dataDumper = PostgresDataDumper.Default;


        [Fact]
        public async Task ShouldDumpDataWhenOptimizeInsertionIsFalse()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("nm", typeof(string));
            var row = table.NewRow();
            row["id"] = 1;
            row["nm"] = "name1";
            table.Rows.Add(row);
            var row2 = table.NewRow();
            row2["id"] = 2;
            row2["nm"] = "name2";
            table.Rows.Add(row2);
            var tableDescriptor = new TableDescriptor
            {
                Schema = "ts",
                Name = "table1",
                Columns = new List<ColumnDescriptor>
                {
                    new ColumnDescriptor
                    {
                        Name = "id", StoreType  = "integer", IsIdentity = true,
                        IdentityInfo = new IdentityDescriptor
                        {
                            IsCyclic =false,
                            Values = new Dictionary<string, object>
                            {
                                [PostgresUtils.IdentityType]="BY DEFAULT",
                            },
                            CurrentValue = 5
                        }
                    },
                    new ColumnDescriptor{Name = "nm",StoreType = "varchar(64)"}
                }
            };
            string tempFile = Path.GetTempFileName();
            await using (var writer = new StreamWriter(tempFile))
            {
                var dumpContext = new DumpContext
                {
                    Setting = new MigrationSetting { OptimizeInsertion = false },
                    Writer = writer

                };
                await _dataDumper.WriteTable(table, tableDescriptor, dumpContext);
                writer.Flush();
            }

            var allLines = File.ReadAllLines(tempFile);
            allLines.Should().BeEquivalentTo(new string[]
            {
                "INSERT INTO \"ts\".\"table1\"(\"id\", \"nm\") OVERRIDING USER VALUE VALUES (1, 'name1');",
                string.Empty,
                "INSERT INTO \"ts\".\"table1\"(\"id\", \"nm\") OVERRIDING USER VALUE VALUES (2, 'name2');",
                string.Empty,
            });
        }

        [Fact]
        public async Task ShouldDumpDataWhenOptimizeInsertionIsTrue()
        {
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
                        Name = "id", StoreType  = "integer", IsIdentity = true,
                        IdentityInfo = new IdentityDescriptor
                        {
                            IsCyclic =false,
                            Values = new Dictionary<string, object>
                            {
                                [PostgresUtils.IdentityType]="BY DEFAULT",
                            },
                            CurrentValue = 5
                        }
                    },
                    new ColumnDescriptor{Name = "nm",StoreType = "varchar(64)"}
                }
            };
            string tempFile = Path.GetTempFileName();

            await using (var writer = new StreamWriter(tempFile))
            {
                var dumpContext = new DumpContext
                {
                    Setting = new MigrationSetting { OptimizeInsertion = true },
                    Writer = writer
                };

                await _dataDumper.WriteTable(table, tableDescriptor, dumpContext);
                writer.Flush();
            }

            // TODO add copy assert
        }
    }
}
