using System.Data.Common;
using ChangeDB.Migration;

namespace ChangeDB
{
    public interface IAgent
    {
        DbConnection CreateConnection(string connectionString);
        IDataMigrator DataMigrator { get; }
        IMetadataMigrator MetadataMigrator { get; }
        IDatabaseManager DatabaseManger { get; }
        AgentSetting AgentSetting { get; }
        IDataDumper DataDumper { get; }
    }
}
