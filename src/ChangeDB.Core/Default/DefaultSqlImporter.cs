using System.Threading.Tasks;
using ChangeDB.Import;

namespace ChangeDB.Default
{
    public class DefaultSqlImporter : IDatabaseSqlImporter
    {
        public Task Import(ImportContext importContext)
        {
            return Task.CompletedTask;
        }
    }
}
