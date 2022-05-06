using System.Threading.Tasks;

namespace ChangeDB.Dump
{
    public interface IDatabaseSqlDumper
    {
        Task DumpSql(DumpSetting dumpSetting, IEventReporter eventReporter);
    }
}
