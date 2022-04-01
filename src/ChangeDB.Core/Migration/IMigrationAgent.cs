using System.Data.Common;

namespace ChangeDB.Migration
{
    public interface IMigrationAgent
    {
        DbConnection CreateConnection(string connectionString);
        IDataMigrator DataMigrator { get; }
        IMetadataMigrator MetadataMigrator { get; }
        IDatabaseManager DatabaseManger { get; }
        IRepr Repr { get; }
        AgentSetting AgentSetting { get; }
        IDataDumper DataDumper { get; }
    }
}
