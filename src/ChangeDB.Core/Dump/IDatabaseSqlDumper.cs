using System.Threading.Tasks;

namespace ChangeDB.Dump
{
    public interface IDatabaseSqlDumper
    {
        Task DumpSql(DumpContext dumpContext);
    }
}
