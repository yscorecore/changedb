using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeConnectionProviderTest
    {
        [Fact]
        public void ShouldGetSqlCeConnection()
        {
            var provider = new SqlCeConnectionProvider();
            provider.CreateConnection("Data Source=MyData.sdf").Should().BeOfType<SqlCeConnection>();
        }
    }
}
