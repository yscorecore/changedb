using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeAgent : IAgent
    {
        public IDataMigrator DataMigrator => SqlCeDataMigrator.Default;
        public IMetadataMigrator MetadataMigrator => SqlCeMetadataMigrator.Default;
        public IDatabaseManager DatabaseManger => SqlCeDatabaseManager.Default;
        public AgentSetting AgentSetting => new AgentSetting { DefaultSchema = null, ObjectNameMaxLength = 128, IdentityName = SqlCeUtils.IdentityName, DatabaseType = "sqlce" };
        public IDataDumper DataDumper => SqlCeDataDumper.Default;

        public IConnectionProvider ConnectionProvider => SqlCeConnectionProvider.Default;

    }
}
