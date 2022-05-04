using System;
using System.Data;
using System.Data.Common;
using ChangeDB.Migration;

namespace ChangeDB
{
    public interface IAgent
    {
        IConnectionProvider ConnectionProvider { get; }
        IDataMigrator DataMigrator { get; }
        IMetadataMigrator MetadataMigrator { get; }
        IDatabaseManager DatabaseManger { get; }
        AgentSetting AgentSetting { get; }
        IDataDumper DataDumper { get; }

        ISqlScriptExecutor SqlExecutor { get; }
    }
}
