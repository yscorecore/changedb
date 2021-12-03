using System.Threading.Tasks;

namespace ChangeDB.Default
{
    public class DefaultSqlFormatter:ISqlFormatter
    {
        public Task<string> FormatSql(string sql)
        {
            return Task.FromResult(sql);
        }
    }
}
