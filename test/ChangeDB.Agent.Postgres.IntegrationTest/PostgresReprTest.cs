using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.Postgres
{

    [Collection(nameof(DatabaseEnvironment))]
    public class PostgresReprTest : BaseTest
    {


        [Theory]
        [InlineData("abc")]
        [InlineData("中国")]
        [InlineData("\r\r\n\n\t\b\f\\")]
        [InlineData("Hello\n中国")]
        public void ShouldReprString(string value)
        {
            using var database = CreateDatabase(true);
            var reprValue = PostgresRepr.Default.ReprValue(value, "varchar");
            var valueFromDatabase = database.Connection.ExecuteScalar<string>($"select {reprValue}");
            valueFromDatabase.Should().Be(value);
        }
    }
}
