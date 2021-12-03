using System.Threading.Tasks;
using ChangeDB.Default;
using Xunit;

namespace ChangeDB.Core.Default
{
    public class DefaultSqlFormatterTest
    {
        private ISqlFormatter _formatter = new DefaultSqlFormatter();
        [Theory]
        [InlineData("insert into")]
        
        public async  Task ShouldGetFormattedSql(string sql)
        {
            var result = await _formatter.FormatSql(sql);
        }
    }
}
