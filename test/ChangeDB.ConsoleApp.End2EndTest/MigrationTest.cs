using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    public class MigrationTest : BaseTest
    {
        private readonly ITestOutputHelper testOutput;

        public MigrationTest(ITestOutputHelper testOutput, TestDatabaseEnvironment testDatabaseEnvironment) : base(testDatabaseEnvironment)
        {
            this.testOutput = testOutput;
        }

        [Theory]
        [MemberData(nameof(GetMigrationTestCases))]
        //[InlineData("sqlserver","Migration/SqlServer/Northwind.sql","sqlce")]
        //[InlineData("sqlserver","Migration/SqlServer/Northwind.sql","mysql")]
        //[InlineData("sqlserver", "Migration/SqlServer/Northwind.sql", "sqlserver")]

        public void ShouldMigrateSuccess(string sourceType, string sourceFile, string targetType)
        {
            using var source = CreateDatabaseFromFile(sourceType, sourceFile);
            using var target = RequestDatabase(targetType);
            var (code, output, error) = RunChangeDbMigration(sourceType, source.ConnectionString, targetType, target.ConnectionString);
            if (code != 0)
            {
                testOutput.WriteLine("Output:");
                testOutput.WriteLine(output);
                testOutput.WriteLine("Error:");
                testOutput.WriteLine(error);
            }
            code.Should().Be(0);
            output.Should().Contain("Execute migration succeeded");
        }
        public static IEnumerable<object[]> GetMigrationTestCases()
        {
            var subDirectories = Directory.GetDirectories("Migration");
            var supportAgents = TestDatabaseEnvironment.SupportedDatabases;
            foreach (var sourceTypeFoler in subDirectories)
            {
                var sourceType = Path.GetFileName(sourceTypeFoler);
                if (!supportAgents.Contains(sourceType, System.StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                foreach (var sqlFile in Directory.GetFiles(sourceTypeFoler, "*.sql"))
                {
                    foreach (var targetType in supportAgents)
                    {
                        yield return new object[] { sourceType, sqlFile, targetType };
                    }
                }
            }
        }
    }
}
