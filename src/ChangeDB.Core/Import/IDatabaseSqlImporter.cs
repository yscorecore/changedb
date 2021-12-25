using System.Threading.Tasks;

namespace ChangeDB.Import
{
    public interface IDatabaseSqlImporter
    {
        Task Import(ImportContext importContext);
    }
}
