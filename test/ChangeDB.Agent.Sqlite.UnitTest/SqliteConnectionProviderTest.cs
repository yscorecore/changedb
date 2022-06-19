using ChangeDB.Agent.Sqlite;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerConnectionProviderTest
    {
        [Fact]
        public void ShouldGetSqlCeConnection()
        {
            var provider = new SqliteConnectionProvider();
            provider.CreateConnection("Data Source=sqlite.db").Should().BeOfType<SqliteConnection>();
        }
    }
}
