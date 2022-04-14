using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{

    public class SqlServerAgent : IAgent
    {
        public IDataMigrator DataMigrator => SqlServerDataMigrator.Default;
        public IMetadataMigrator MetadataMigrator => SqlServerMetadataMigrator.Default;
        public IDatabaseManager DatabaseManger => SqlServerDatabaseManager.Default;
        public AgentSetting AgentSetting => new AgentSetting { ObjectNameMaxLength = 128, DefaultSchema = "dbo", SupportSchema = true, IdentityName = SqlServerUtils.IdentityName, DatabaseType = "sqlserver", ScriptSplit = "go" };
        public IDataDumper DataDumper => SqlServerDataDumper.Default;

        public IConnectionProvider ConnectionProvider => SqlServerConnectionProvider.Default;

    }
}
