using ChangeDB.Agent.Postgres;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class PostgresConnectionProviderTest
    {
        [Fact]
        public void ShouldGetNpgSqlConnection()
        {
            var provider = new PostgresConnectionProvider();
            provider.CreateConnection("Server=127.0.0.1;Port=5432;User Id=postgres;Password=mypassword;").Should().BeOfType<NpgsqlConnection>();
        }
    }
}
