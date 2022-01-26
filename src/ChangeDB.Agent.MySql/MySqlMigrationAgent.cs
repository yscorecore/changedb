using System.Data.Common;
using ChangeDB.Migration;
using MySqlConnector;

namespace ChangeDB.Agent.MySql
{
    public class MySqlMigrationAgent : IMigrationAgent
    {
        public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

        public IDataMigrator DataMigrator => MySqlDataMigrator.Default;

        public IMetadataMigrator MetadataMigrator => MySqlMetadataMigrator.Default;

        public IDataTypeMapper DataTypeMapper => MySqlDataTypeMapper.Default;

        public ISqlExpressionTranslator ExpressionTranslator => MySqlExpressionTranslator.Default;

        public IDatabaseManager DatabaseManger => MySqlDatabaseManager.Default;

        public IRepr Repr => MySqlRepr.Default;

        public AgentSetting AgentSetting => new AgentSetting
        { SupportSchema = false, DefaultSchema = null, ObjectNameMaxLength = 64 };
    }
}
