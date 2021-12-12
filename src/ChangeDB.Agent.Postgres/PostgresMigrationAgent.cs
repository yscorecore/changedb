using System.Data.Common;
using ChangeDB.Migration;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator { get => PostgresDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => PostgresMetadataMigrator.Default; }
        public IDataTypeMapper DataTypeMapper { get => PostgresDataTypeMapper.Default; }
        public ISqlExpressionTranslator ExpressionTranslator { get => PostgresSqlExpressionTranslator.Default; }
        public IDatabaseManager DatabaseManger { get => PostgresDatabaseManager.Default; }
        public AgentSetting AgentSetting { get => new AgentSetting { ObjectNameMaxLength = 64, DefaultSchema = "public", SupportSchema = true }; }
        public IRepr Repr { get => PostgresRepr.Default; }

        public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    }
}
