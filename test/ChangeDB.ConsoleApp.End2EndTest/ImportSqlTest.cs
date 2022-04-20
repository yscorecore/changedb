using System;
using System.IO;
using FluentAssertions;
using TestDB;
using Xunit;
using Xunit.Abstractions;
using static ChangeDB.ConsoleApp.End2EndTest.End2EndUtils;
using static TestDB.Databases;


namespace ChangeDB.ConsoleApp.End2EndTest
{
    public class ImportSqlTest : BaseTest
    {
        private Action<string> WriteLine { get; }

        public ImportSqlTest(ITestOutputHelper testOutput)
        {
            WriteLine = testOutput.WriteLine;
        }

        [Theory(Skip = "not finished")]
        [MemberData(nameof(GetTestCases))]
        public void ShouldImportDumpFile(string sourceType, string sourceFile, string targetType)
        {
            using var source = CreateDatabaseFromFile(sourceType, true, sourceFile);
            using var tempFile = new TempFile();
            RunChangeDumpSql(sourceType, source.ConnectionString, targetType, tempFile.FilePath);

            using var target = CreateDatabase(sourceType, false);
            var (code, output, error) = RunChangeImportSql(sourceType, target.ConnectionString, tempFile.FilePath);
            if (code != 0)
            {
                WriteLine("Import Output:");
                WriteLine(output);
                WriteLine("Import Error:");
                WriteLine(error);
            }
            code.Should().Be(0);
            output.Should().Contain("Execute importsql succeeded");
        }

        [Theory(Skip = "not finished")]
        [MemberData(nameof(GetScriptFiles))]
        public void ShouldImportScriptFiles(string dbType, string sourceFile)
        {
            var importFile = Path.GetFullPath(sourceFile);
            using var target = CreateDatabase(dbType, false);
            var (code, output, error) = RunChangeImportSql(dbType, target.ConnectionString, importFile);
            if (code != 0)
            {
                WriteLine("Output:");
                WriteLine(output);
                WriteLine("Error:");
                WriteLine(error);
            }
            code.Should().Be(0);
            output.Should().Contain("Execute importsql succeeded");


        }

    }
}
