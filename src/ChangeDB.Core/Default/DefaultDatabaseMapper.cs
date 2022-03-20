using System.Threading.Tasks;
using ChangeDB.Migration;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Default
{
    public class DefaultDatabaseMapper : IDatabaseMapper
    {
        public Task<DatabaseDescriptorMapper> MapDatabase(DatabaseDescriptor sourceDatabase)
        {
            throw new System.NotImplementedException();
        }
    }
}
