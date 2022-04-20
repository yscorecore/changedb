using ChangeDB.Agent.MySql;
using FluentAssertions;
using MySqlConnector;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class MySqlConnectionProviderTest
    {
        [Fact]
        public void ShouldGetMySqlConnection()
        {
            var provider = new MysqlConnectionProvider();
            provider.CreateConnection("Server=127.0.0.1;Port=3306;Uid=root;Pwd=password;").Should().BeOfType<MySqlConnection>();
        }
    }
}
