using System.Data.Common;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{

    [Service(typeof(IMigrationAgent), Name = "sqlserver")]
    public class SqlServerMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator { get => SqlServerDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => SqlServerMetadataMigrator.Default; }
        public IDataTypeMapper DataTypeMapper { get => SqlServerDataTypeMapper.Default; }
        public ISqlExpressionTranslator ExpressionTranslator { get => SqlServerSqlExpressionTranslator.Default; }
        public IDatabaseManager DatabaseManger { get => SqlServerDatabaseManager.Default; }
        public AgentSetting AgentSetting { get => new AgentSetting { ObjectNameMaxLength = 128, DefaultSchema = "dbo", SupportSchema = true }; }
        public IRepr Repr { get => SqlServerRepr.Default; }

        public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    }
}
