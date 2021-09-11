
using FluentAssertions;
using Xunit;

namespace Hello.UnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void TestMethod1()
        {
            var c1 = new Class1();
            var result = c1.Say("world");
            result.Should().Be("Hello, world.");
        }

    }
}
