using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.Postgres
{

    [Collection(nameof(DatabaseEnvironment))]
    public class PostgresReprTest
    {
        private readonly DatabaseEnvironment _databaseEnvironment;
        private readonly IRepr _repr = PostgresRepr.Default;
        public PostgresReprTest(DatabaseEnvironment databaseEnvironment)
        {
            _databaseEnvironment = databaseEnvironment;
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("中国")]
        [InlineData("\r\r\n\n\t\b\f\\")]
        [InlineData("Hello\n中国")]
        public void ShouldReprString(string value)
        {
            var reprValue = _repr.ReprValue(value, "varchar");
            var valueFromDatabase = _databaseEnvironment.DbConnection.ExecuteScalar<string>($"select {reprValue}");
            valueFromDatabase.Should().Be(value);
        }
    }
}
