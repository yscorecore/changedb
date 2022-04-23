using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Microsoft.Extensions.DependencyInjection;
using TestDB;
using Xunit;

namespace ChangeDB
{
    public class MigrationTest : BaseTest
    {

        [Theory]
        [MemberData(nameof(AllTestCases))]
        public async Task ShouldMigrationSuccess(string testcase, string sourceType, string databaseName, string targetType)
        {
            var caseFolder = Path.Combine("testcases", "migration", testcase);


            using var sourceDatabase = Databases.CreateDatabaseFromFile(sourceType, true, GetDatabaseFile(sourceType, databaseName));
            using var targetDatabase = Databases.RequestDatabase(targetType);
            var migrationContext = new MigrationContext()
            {
                Setting = CreateSetting(caseFolder),
                SourceDatabase = new DatabaseInfo() { DatabaseType = sourceType, ConnectionString = sourceDatabase.ConnectionString },
                TargetDatabase = new DatabaseInfo() { DatabaseType = targetType, ConnectionString = targetDatabase.ConnectionString },
            };
            var migrate = ServiceProvider.GetRequiredService<IDatabaseMigrate>();
            await migrate.MigrateDatabase(migrationContext);

            var targetFolder = Path.Combine(caseFolder, $"{sourceType}_{databaseName}_{targetType}");
            await AssertTargetResult(targetType, targetDatabase, targetFolder);

        }

        public static IEnumerable<object[]> AllTestCases()
        {
            var rootFolder = Path.Combine("testcases", "migration");
            foreach (var testcasePath in Directory.GetDirectories(rootFolder))
            {
                var testcaseName = Path.GetFileName(testcasePath);
                foreach (var dumpPath in Directory.GetDirectories(testcasePath))
                {
                    var dumpInfo = Path.GetFileName(dumpPath);
                    var items = dumpInfo.Split('_');
                    if (items.Length == 3)
                    {
                        yield return new object[] { testcaseName }.Concat(items).ToArray();
                    }
                }
            }
        }


        private MigrationSetting CreateSetting(string caseFolder)
        {
            var settingFile = Path.Combine(caseFolder, "settings.json");
            return ReadFromDataFile<MigrationSetting>(settingFile);
        }
        private async Task AssertTargetResult(string targetType, IDatabase database, string targetCaseFolder)
        {
            var agent = ServiceProvider.GetService<IAgentFactory>().CreateAgent(targetType);
            var databaseDesc = await GetTargetDatabaseDesc(agent, database.Connection);
            var databaseData = await GetTargetDatabaseData(agent, databaseDesc, database.Connection);
            var dataFile = Path.Combine(targetCaseFolder, "data.json");
            var schemaFile = Path.Combine(targetCaseFolder, "schema.json");
            if (WriteMode)
            {
                WriteToDataFile(databaseDesc, schemaFile);
                WriteToDataFile(databaseData, dataFile);
            }
            // ignore extension object compare and name compare
            // Tables[0].PrimaryKey.Name
            // Tables[0].Indexes[0].Name
            // Tables[0].ForeignKeys[0].Name
            // Tables[0].Uniques[0].Name
            ShouldBeDataFile(databaseDesc, schemaFile, config =>
                 config
                    .Excluding(p => typeof(ExtensionObject).IsAssignableFrom(p.DeclaringType) && p.Name == nameof(ExtensionObject.Values))
                    .Excluding(p => Regex.IsMatch(p.Path, @"Tables\[\d+\].\w+(\[\d+\])?.Name"))
            );
            ShouldBeDataFile(databaseData, dataFile);
        }
        private Task<DatabaseDescriptor> GetTargetDatabaseDesc(IAgent agent, DbConnection dbConnection)
        {
            var context = new MigrationContext
            {
                Setting = new MigrationSetting(),
                SourceConnection = dbConnection
            };
            return agent.MetadataMigrator.GetSourceDatabaseDescriptor(context);
        }
        private Task<Dictionary<string, TableInfo>> GetTargetDatabaseData(IAgent agent, DatabaseDescriptor databaseDesc, DbConnection dbConnection)
        {
            var databaseData = new Dictionary<string, TableInfo>();
            var tables = databaseDesc.Tables.OrderBy(p => p.Schema).ThenBy(p => p.Name);
            foreach (var table in tables)
            {
                var tableKey = string.IsNullOrEmpty(table.Schema) ? table.Name : $"{table.Schema}.{table.Name}";
                var countSql = $"select count(*) from {agent.AgentSetting.IdentityName(table.Schema, table.Name)}";
                var count = dbConnection.ExecuteScalar<long>(countSql);
                var sql = $"select * from {agent.AgentSetting.IdentityName(table.Schema, table.Name)}";
                using var reader = dbConnection.ExecuteReader(sql);
                var columns = reader.GetSchemaTable().Rows.OfType<DataRow>()
                        .Select(p => new
                        {
                            Name = p.FieldValue<string>("ColumnName"),
                            Type = p.FieldValue<Type>("DataType").FullName
                        })
                        .ToList();
                var rows = reader.LoadDataArray();
                databaseData.Add(tableKey,
                    new TableInfo
                    {
                        RowCount = count,
                        Rows = rows.ToList(),
                        Columns = columns.ToDictionary(p => p.Name, p => p.Type)
                    });
            }
            return Task.FromResult(databaseData);
        }

        private class TableInfo
        {
            public Dictionary<string, string> Columns { get; set; }
            public long RowCount { get; set; }
            public List<object[]> Rows { get; set; }
        }
    }
}
