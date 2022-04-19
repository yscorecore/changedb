using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerConnectionProviderTest
    {
        [Fact]
        public void ShouldGetSqlCeConnection()
        {
            var provider = new SqlServerConnectionProvider();
            provider.CreateConnection("Server=127.0.0.1,;User Id=sa;Password=mypassword;").Should().BeOfType<SqlConnection>();
        }
    }
}
