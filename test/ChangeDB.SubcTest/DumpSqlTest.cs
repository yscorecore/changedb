using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChangeDB.Dump;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TestDB;
using Xunit;

namespace ChangeDB
{
    public class DumpSqlTest : BaseTest
    {


        [Theory]
        [MemberData(nameof(AllTestCases))]
        public async Task ShouldDumpSuccess(string testcase, string sourceType, string databaseName, string targetType)
        {
            var caseFolder = Path.Combine("testcases", "dumpsql", testcase);
            var dumper = ServiceProvider.GetRequiredService<IDatabaseSqlDumper>();
            using var sourceDatabase = Databases.CreateDatabaseFromFile(sourceType, true, GetDatabaseFile(sourceType, databaseName));
            using var tempFile = new TempFile();
            using (var streamWriter = tempFile.GetStreamWriter())
            {
                var dumpContext = new DumpContext()
                {
                    Writer = streamWriter,
                    Setting = CreateSetting(caseFolder),
                    SourceDatabase = new DatabaseInfo() { DatabaseType = sourceType, ConnectionString = sourceDatabase.ConnectionString },
                    TargetDatabase = new DatabaseInfo() { DatabaseType = targetType, ConnectionString = string.Empty },
                };
                await dumper.DumpSql(dumpContext);
                streamWriter.Flush();
            }
            var expectedFile = Path.Combine(caseFolder, $"{sourceType}_{databaseName}_{targetType}", "dump.sql");
            AssertDumpfile(tempFile.FilePath, expectedFile);
        }
        private MigrationSetting CreateSetting(string caseFolder)
        {
            var settingFile = Path.Combine(caseFolder, "settings.json");
            return ReadFromDataFile<MigrationSetting>(settingFile);
        }
        private void AssertDumpfile(string filePath, string expectedFile)
        {
            if (WriteMode)
            {
                File.Copy(filePath, expectedFile, true);
            }
            File.Exists(expectedFile).Should().BeTrue($"should exists expected file '{expectedFile}'");
            var expectedContent = string.Join(Environment.NewLine, File.ReadAllLines(expectedFile)).Trim();
            var currentContent = string.Join(Environment.NewLine, File.ReadAllLines(filePath)).Trim();
            currentContent.Should().Be(expectedContent, "the dump file should same");
        }
        public static IEnumerable<object[]> AllTestCases()
        {
            var rootFolder = Path.Combine("testcases", "dumpsql");

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

    }
}
