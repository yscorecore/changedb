using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface ISqlExecuter
    {
        Task ExecuteNoQuery(string sql);
    }
}
