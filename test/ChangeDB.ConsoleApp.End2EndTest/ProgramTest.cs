using FluentAssertions;
using Xunit;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    public class ProgramTest
    {
        [Fact]
        public void ShouldPrintHelpMessageWhenNoCommandProvided()
        {
            var (exitCode, output, _) = RunChangeDbApp(string.Empty);
            exitCode.Should().Be(1);
            output.Should().Contain("No verb selected");
        }


        private static (int, string, string) RunChangeDbApp(string arguments)
        {
            return Shell.Exec("dotnet", $"ChangeDB.ConsoleApp.dll {arguments}");
        }
        private static (int, string, string) RunChangeDbMigration(string arguments)
        {
            return RunChangeDbApp($"migration {arguments}");
        }
        private static (int, string, string) RunChangeDumpSql(string arguments)
        {
            return RunChangeDbApp($"migration {arguments}");
        }
        private static (int, string, string) RunChangeImportSql(string arguments)
        {
            return RunChangeDbApp($"migration {arguments}");
        }
    }
}
