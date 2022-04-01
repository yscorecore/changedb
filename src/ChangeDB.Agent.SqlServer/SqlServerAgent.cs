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
        public AgentSetting AgentSetting => new AgentSetting { ObjectNameMaxLength = 128, DefaultSchema = "dbo", SupportSchema = true, IdentityName = SqlServerUtils.IdentityName };
        public IDataDumper DataDumper => SqlServerDataDumper.Default;
        public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    }
}
