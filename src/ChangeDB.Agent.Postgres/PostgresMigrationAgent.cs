using System.Data.Common;
using ChangeDB.Migration;
using YS.Knife;

namespace ChangeDB.Agent.Postgres
{
    [DictionaryKey("postgres")]
    [Service]
    public class PostgresMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator { get => PostgresDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => PostgresMetadataMigrator.Default; }
        public IDataTypeMapper DataTypeMapper { get => PostgresDataTypeMapper.Default; }
        public ISqlExpressionTranslator ExpressionTranslator { get => PostgresSqlExpressionTranslator.Default; }
        public IDatabaseManager DatabaseManger { get=> PostgresDatabaseManager.Default; }

        public DbConnection CreateConnection(string connectionString)
        {
            return new Npgsql.NpgsqlConnection(connectionString);
        }
    }
}
