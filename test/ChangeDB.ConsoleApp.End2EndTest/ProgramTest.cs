using FluentAssertions;
using Xunit;

using static ChangeDB.ConsoleApp.End2EndTest.End2EndUtils;
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
    }
}
