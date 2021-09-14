using System.Data;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IMigrationAgent
    {
        IDataMigrator  DataMigrator { get; }
        IMetadataMigrator MetadataMigrator { get; }
    }
}
