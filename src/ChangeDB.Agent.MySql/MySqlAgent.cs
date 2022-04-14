using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using MySqlConnector;

namespace ChangeDB.Agent.MySql
{
    public class MySqlAgent : IAgent
    {

        public IDataMigrator DataMigrator => MySqlDataMigrator.Default;

        public IMetadataMigrator MetadataMigrator => MySqlMetadataMigrator.Default;

        public IDatabaseManager DatabaseManger => MySqlDatabaseManager.Default;

        public AgentSetting AgentSetting => new AgentSetting
        { SupportSchema = false, DefaultSchema = null, ObjectNameMaxLength = 64, DatabaseType = "mysql", ScriptSplit = "", IdentityName = MySqlUtils.IdentityName };

        public IDataDumper DataDumper => MySqlDataDumper.Default;

        public IConnectionProvider ConnectionProvider => MysqlConnectionProvider.Default;
    }
}
