using System.Data.Common;
using ChangeDB.Migration;
using MySqlConnector;

namespace ChangeDB.Agent.MySql
{
    public class MySqlAgent : IAgent
    {
        public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

        public IDataMigrator DataMigrator => MySqlDataMigrator.Default;

        public IMetadataMigrator MetadataMigrator => MySqlMetadataMigrator.Default;

        public IDatabaseManager DatabaseManger => MySqlDatabaseManager.Default;

        public AgentSetting AgentSetting => new AgentSetting
        { SupportSchema = false, DefaultSchema = null, ObjectNameMaxLength = 64 };

        public IDataDumper DataDumper => MySqlDataDumper.Default;
    }
}
