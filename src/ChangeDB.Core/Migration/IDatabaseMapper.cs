using System.Threading.Tasks;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Migration
{
    public interface IDatabaseMapper
    {
        Task<DatabaseDescriptorMapper> MapDatabase(DatabaseDescriptor sourceDatabase);
    }
}
