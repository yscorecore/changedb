using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using TestDB;
using Xunit;
using Xunit.Abstractions;
using static ChangeDB.ConsoleApp.End2EndTest.End2EndUtils;
using static TestDB.Databases;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    public class DumpSqlTest : BaseEnd2EndTest
    {
        private Action<string> WriteLine { get; }

        public DumpSqlTest(ITestOutputHelper testOutput)
        {
            WriteLine = testOutput.WriteLine;
        }

        [Theory]
        [MemberData(nameof(GetTestCases))]
        public void ShouldDumpSuccess(string sourceType, string sourceFile, string targetType)
        {
            using var source = CreateDatabaseFromFile(sourceType, true, sourceFile);
            using var tempFile = new TempFile();
            var (code, output, error) = RunChangeDumpSql(sourceType, source.ConnectionString, targetType, tempFile.FilePath);
            if (code != 0)
            {
                WriteLine("Output:");
                WriteLine(output);
                WriteLine("Error:");
                WriteLine(error);
            }
            code.Should().Be(0);
            output.Should().Contain("Execute dumpsql succeeded");
        }


    }
}
