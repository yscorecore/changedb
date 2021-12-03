using System.Threading.Tasks;

namespace ChangeDB
{
    public interface ISqlFormatter
    {
        Task<string> FormatSql(string sql);
    }
}
