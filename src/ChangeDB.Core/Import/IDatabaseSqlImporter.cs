using System.Threading.Tasks;

namespace ChangeDB.Import
{
    public interface IDatabaseSqlImporter
    {
        Task Import(ImportSetting importSetting, IEventReporter eventReporter);
    }
}
