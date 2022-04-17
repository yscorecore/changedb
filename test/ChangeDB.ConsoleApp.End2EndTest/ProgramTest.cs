using FluentAssertions;
using Xunit;

namespace ChangeDB.ConsoleApp.End2EndTest
{
    public class ProgramTest : BaseTest
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
