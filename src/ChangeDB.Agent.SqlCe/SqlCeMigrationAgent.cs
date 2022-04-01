using System.Data.Common;
using System.Data.SqlServerCe;
using ChangeDB.Agent.SqlServer;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator => SqlCeDataMigrator.Default;
        public IMetadataMigrator MetadataMigrator => SqlCeMetadataMigrator.Default;
        public IDatabaseManager DatabaseManger => SqlCeDatabaseManager.Default;
        public AgentSetting AgentSetting => new AgentSetting { DefaultSchema = null, ObjectNameMaxLength = 128, IdentityName = SqlCeUtils.IdentityName };
        public IRepr Repr => SqlServerRepr.Default;
        public IDataDumper DataDumper => SqlServerDataDumper.Default;

        public DbConnection CreateConnection(string connectionString) => new SqlCeConnection(connectionString);

    }
}
