using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static ChangeDB.ConsoleApp.End2EndTest.End2EndUtils;
using static TestDB.Databases;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    public class MigrationTest : BaseTest
    {
        private Action<string> WriteLine { get; }

        public MigrationTest(ITestOutputHelper testOutput)
        {
            WriteLine = testOutput.WriteLine;
        }

        [Theory]
        [MemberData(nameof(GetTestCases))]
        public void ShouldMigrateSuccess(string sourceType, string sourceFile, string targetType)
        {
            using var source = CreateDatabaseFromFile(sourceType, true, sourceFile);
            using var target = RequestDatabase(targetType);
            var (code, output, error) = RunChangeDbMigration(sourceType, source.ConnectionString, targetType, target.ConnectionString);
            if (code != 0)
            {
                WriteLine("Output:");
                WriteLine(output);
                WriteLine("Error:");
                WriteLine(error);
            }
            code.Should().Be(0);
            output.Should().Contain("Execute migration succeeded");
        }

    }
}
